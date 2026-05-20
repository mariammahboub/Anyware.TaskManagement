using Anyware.TaskManagement.Application.Common.Exceptions;
using Anyware.TaskManagement.Application.Common.Interfaces;
using Anyware.TaskManagement.Application.Features.Tasks.Commands.CreateTask;
using Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.DTOs;
using Anyware.TaskManagement.Domain.Entities;
using Anyware.TaskManagement.Domain.Enums;
using Anyware.TaskManagement.Domain.Interfaces;
using Anyware.TaskManagement.Domain.Interfaces.Repositories;
using AutoMapper;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Tests.Features.Tasks
{
    public sealed class CreateTaskCommandHandlerTests
    {

        private readonly Mock<ITaskRepository> _taskRepo = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly Mock<ITaskQueue> _taskQueue = new();
        private readonly Mock<IMapper> _mapper = new();

        private readonly Guid _userId = Guid.NewGuid();

        private CreateTaskCommandHandler CreateHandler() => new(
            _taskRepo.Object,
            _unitOfWork.Object,
            _currentUser.Object,
            _taskQueue.Object,
            _mapper.Object);

        public CreateTaskCommandHandlerTests()
        {
            _currentUser.Setup(u => u.UserId).Returns(_userId);
            _currentUser.Setup(u => u.IsAuthenticated).Returns(true);
            _taskRepo
                .Setup(r => r.ExistsByTitleAndUserAndDateAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _taskRepo
                .Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _unitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _mapper
                .Setup(m => m.Map<TaskDto>(It.IsAny<TaskItem>()))
                .Returns((TaskItem t) => new TaskDto(
                    t.Id, t.Title, t.Description,
                    t.Status.ToString(), t.Priority.ToString(),
                    t.UserId, t.CreatedAt, t.UpdatedAt));
        }
        [Fact]
        public async Task Handle_ValidCommand_ReturnsTaskDtoWithCorrectValues()
        {
            var command = new CreateTaskCommand("Fix bug", "Details here", TaskPriority.High);
            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);
            result.Should().NotBeNull();
            result.Title.Should().Be("Fix bug");
            result.Priority.Should().Be("High");
            result.Status.Should().Be("Pending");
            result.UserId.Should().Be(_userId);
        }

        [Fact]
        public async Task Handle_ValidCommand_CallsAddAsyncOnRepository()
        {
            var handler = CreateHandler();
            var command = new CreateTaskCommand("New Task", "Desc", TaskPriority.Low);

            await handler.Handle(command, CancellationToken.None);

            _taskRepo.Verify(r =>
                r.AddAsync(
                    It.Is<TaskItem>(t =>
                        t.Title == "New Task" &&
                        t.UserId == _userId &&
                        t.Status == TaskItemStatus.Pending),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_CallsSaveChangesExactlyOnce()
        {
            var handler = CreateHandler();
            await handler.Handle(
                new CreateTaskCommand("Task", "Desc", TaskPriority.Medium),
                CancellationToken.None);

            _unitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_EnqueuesTaskForBackgroundProcessing()
        {
            var handler = CreateHandler();
            TaskItem? capturedTask = null;

            _taskRepo
                .Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
                .Callback<TaskItem, CancellationToken>((t, _) => capturedTask = t)
                .Returns(Task.CompletedTask);

            await handler.Handle(
                new CreateTaskCommand("Task", "Desc", TaskPriority.Critical),
                CancellationToken.None);
            _taskQueue.Verify(q =>
                q.Enqueue(capturedTask!.Id),
                Times.Once);
        }
        [Fact]
        public async Task Handle_DuplicateTitleSameDay_ThrowsConflictException()
        {
            _taskRepo
                .Setup(r => r.ExistsByTitleAndUserAndDateAsync(
                    "Fix bug",
                    _userId,
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = CreateHandler();

            var act = async () => await handler.Handle(
                new CreateTaskCommand("Fix bug", "Desc", TaskPriority.Low),
                CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("*Fix bug*today*");
        }

        [Fact]
        public async Task Handle_DuplicateTitle_NeverCallsSaveChanges()
        {
            _taskRepo
                .Setup(r => r.ExistsByTitleAndUserAndDateAsync(
                    It.IsAny<string>(), It.IsAny<Guid>(),
                    It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = CreateHandler();

            await FluentActions
                .Invoking(() => handler.Handle(
                    new CreateTaskCommand("Dup", "Desc", TaskPriority.Low),
                    CancellationToken.None))
                .Should().ThrowAsync<ConflictException>();

            _unitOfWork.Verify(u =>
                u.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_DuplicateTitle_NeverEnqueuesTask()
        {
            _taskRepo
                .Setup(r => r.ExistsByTitleAndUserAndDateAsync(
                    It.IsAny<string>(), It.IsAny<Guid>(),
                    It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = CreateHandler();

            await FluentActions
                .Invoking(() => handler.Handle(
                    new CreateTaskCommand("Dup", "Desc", TaskPriority.Low),
                    CancellationToken.None))
                .Should().ThrowAsync<ConflictException>();

            _taskQueue.Verify(q => q.Enqueue(It.IsAny<Guid>()), Times.Never);
        }
    }
}