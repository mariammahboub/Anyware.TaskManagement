using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Anyware.TaskManagement.Application.Features.Tasks.Commands.CreateTask;
using Anyware.TaskManagement.Application.Features.Tasks.Commands.UpdateTaskStatus;

using Anyware.TaskManagement.Domain.Enums;
using Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.DTOs;
using Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.GetAllTasks;
using Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.GetTaskById;
using Anyware.TaskManagement.API.Controller;

namespace Anyware.TaskManagement.API.Tests.Controllers;

public sealed class TasksControllerTests
{
    private readonly Mock<ISender>   _sender     = new();
    private readonly TasksController _controller;

    private static readonly Guid    TaskId    = Guid.NewGuid();
    private static readonly TaskDto SampleDto = new(
        TaskId, "Sample Task", "Description",
        "Pending", "High", Guid.NewGuid(),
        DateTime.UtcNow, null);

    public TasksControllerTests()
    {
        _controller = new TasksController(_sender.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task CreateTask_ValidRequest_Returns201WithTaskDto()
    {
        _sender
            .Setup(s => s.Send(It.IsAny<CreateTaskCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto);

        var result = await _controller.CreateTask(
            new CreateTaskRequest("Sample Task", "Description", TaskPriority.High),
            CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.Value.Should().BeEquivalentTo(SampleDto);
    }

    [Fact]
    public async Task CreateTask_DispatchesCorrectCommand()
    {
        _sender
            .Setup(s => s.Send(It.IsAny<CreateTaskCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto);

        await _controller.CreateTask(
            new CreateTaskRequest("My Task", "My Desc", TaskPriority.Critical),
            CancellationToken.None);

        _sender.Verify(s =>
            s.Send(
                It.Is<CreateTaskCommand>(c =>
                    c.Title       == "My Task"           &&
                    c.Description == "My Desc"           &&
                    c.Priority    == TaskPriority.Critical),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateTask_SetsLocationHeaderToGetTaskByIdRoute()
    {
        _sender
            .Setup(s => s.Send(It.IsAny<CreateTaskCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto);

        var result  = await _controller.CreateTask(
            new CreateTaskRequest("T", "D", TaskPriority.Low),
            CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(TasksController.GetTaskById));
        created.RouteValues!["id"].Should().Be(SampleDto.Id);
    }

    [Fact]
    public async Task GetTaskById_Returns200WithTaskDto()
    {
        _sender
            .Setup(s => s.Send(It.IsAny<GetTaskByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto);

        var result   = await _controller.GetTaskById(TaskId, CancellationToken.None);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;

        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(SampleDto);
    }

    [Fact]
    public async Task GetTaskById_DispatchesQueryWithCorrectId()
    {
        var id = Guid.NewGuid();
        _sender
            .Setup(s => s.Send(It.IsAny<GetTaskByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto);

        await _controller.GetTaskById(id, CancellationToken.None);

        _sender.Verify(s =>
            s.Send(
                It.Is<GetTaskByIdQuery>(q => q.TaskId == id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllTasks_Returns200WithList()
    {
        var tasks = new List<TaskDto> { SampleDto };
        _sender
            .Setup(s => s.Send(It.IsAny<GetAllTasksQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks.AsReadOnly());

        var result   = await _controller.GetAllTasks(CancellationToken.None);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;

        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(tasks);
    }

    [Fact]
    public async Task GetAllTasks_EmptyList_Returns200()
    {
        _sender
            .Setup(s => s.Send(It.IsAny<GetAllTasksQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TaskDto>().AsReadOnly());

        var result   = await _controller.GetAllTasks(CancellationToken.None);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;

        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        (okResult.Value as IEnumerable<TaskDto>).Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTaskStatus_ValidRequest_Returns204()
    {
  
        _sender
            .Setup(s => s.Send(It.IsAny<UpdateTaskStatusCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.UpdateTaskStatus(
            TaskId,
            new UpdateTaskStatusRequest(TaskItemStatus.InProgress),
            CancellationToken.None);

        result.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task UpdateTaskStatus_DispatchesCorrectCommand()
    {
        var id = Guid.NewGuid();
        _sender
            .Setup(s => s.Send(It.IsAny<UpdateTaskStatusCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _controller.UpdateTaskStatus(
            id,
            new UpdateTaskStatusRequest(TaskItemStatus.Done),
            CancellationToken.None);

        _sender.Verify(s =>
            s.Send(
                It.Is<UpdateTaskStatusCommand>(c =>
                    c.TaskId    == id                  &&
                    c.NewStatus == TaskItemStatus.Done),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
