using Anyware.TaskManagement.Application.Common.Exceptions;
using Anyware.TaskManagement.Application.Common.Interfaces;
using Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.DTOs;
using Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.GetTaskById;
using Anyware.TaskManagement.Domain.Entities;
using Anyware.TaskManagement.Domain.Enums;
using Anyware.TaskManagement.Domain.Interfaces.Repositories;
using AutoMapper;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Tests.Features.Queries
{
    public sealed class GetTaskByIdQueryHandlerTests
    {
        private readonly Mock<ITaskRepository> _taskRepo = new();
        private readonly Mock<ICacheService> _cache = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly Mock<IMapper> _mapper = new();

        private readonly Guid _ownerId = Guid.NewGuid();
        private readonly Guid _taskId = Guid.NewGuid();
        private readonly TaskItem _task;
        private readonly TaskDto _taskDto;

        private GetTaskByIdQueryHandler CreateHandler() => new(
            _taskRepo.Object,
            _cache.Object,
            _currentUser.Object,
            _mapper.Object);

        public GetTaskByIdQueryHandlerTests()
        {
            _task = TaskItem.Create("Test Task", "Description", TaskPriority.High, _ownerId);
            _taskDto = new TaskDto(
                _task.Id, _task.Title, _task.Description,
                _task.Status.ToString(), _task.Priority.ToString(),
                _ownerId, _task.CreatedAt, null);
            _currentUser.Setup(u => u.UserId).Returns(_ownerId);
            _currentUser.Setup(u => u.IsAdmin).Returns(false);
            _mapper.Setup(m => m.Map<TaskDto>(_task)).Returns(_taskDto);
        }
        [Fact]
        public async Task Handle_CacheHit_ReturnsCachedDtoWithoutHittingDatabase()
        {
            _cache
                .Setup(c => c.GetAsync<TaskDto>(
                    $"task:{_task.Id}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(_taskDto);

            var result = await CreateHandler()
                .Handle(new GetTaskByIdQuery(_task.Id), CancellationToken.None);

            result.Should().BeEquivalentTo(_taskDto);
            _taskRepo.Verify(r =>
                r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
        [Fact]
        public async Task Handle_CacheMiss_LoadsFromDatabaseAndPopulatesCache()
        {
            _cache
                .Setup(c => c.GetAsync<TaskDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TaskDto?)null);

            _taskRepo
                .Setup(r => r.GetByIdAsync(_task.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_task);

            var result = await CreateHandler()
                .Handle(new GetTaskByIdQuery(_task.Id), CancellationToken.None);

            result.Should().BeEquivalentTo(_taskDto);
            _cache.Verify(c =>
                c.SetAsync(
                    $"task:{_task.Id}",
                    _taskDto,
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        [Fact]
        public async Task Handle_TaskNotFound_ThrowsNotFoundException()
        {
            _cache
                .Setup(c => c.GetAsync<TaskDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TaskDto?)null);
            _taskRepo
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TaskItem?)null);

            var act = async () => await CreateHandler()
                .Handle(new GetTaskByIdQuery(Guid.NewGuid()), CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }
        [Fact]
        public async Task Handle_TaskBelongsToDifferentUser_ThrowsForbiddenException()
        {
            var differentUserId = Guid.NewGuid();
            _currentUser.Setup(u => u.UserId).Returns(differentUserId);
            _currentUser.Setup(u => u.IsAdmin).Returns(false);

            _cache
                .Setup(c => c.GetAsync<TaskDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TaskDto?)null);
            _taskRepo
                .Setup(r => r.GetByIdAsync(_task.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_task);

            var act = async () => await CreateHandler()
                .Handle(new GetTaskByIdQuery(_task.Id), CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Handle_AdminCanAccessAnyTask_ReturnsDto()
        {
            _currentUser.Setup(u => u.UserId).Returns(Guid.NewGuid());
            _currentUser.Setup(u => u.IsAdmin).Returns(true);

            _cache
                .Setup(c => c.GetAsync<TaskDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TaskDto?)null);
            _taskRepo
                .Setup(r => r.GetByIdAsync(_task.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_task);

            var result = await CreateHandler()
                .Handle(new GetTaskByIdQuery(_task.Id), CancellationToken.None);

            result.Should().NotBeNull();
        }
        [Fact]
        public async Task Handle_CacheHit_StillEnforcesOwnershipOnCachedDto()
        {
            var differentUserId = Guid.NewGuid();
            _currentUser.Setup(u => u.UserId).Returns(differentUserId);
            _currentUser.Setup(u => u.IsAdmin).Returns(false);
            _cache
                .Setup(c => c.GetAsync<TaskDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_taskDto);

            var act = async () => await CreateHandler()
                .Handle(new GetTaskByIdQuery(_task.Id), CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenException>();
        }
    }
}