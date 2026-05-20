using Anyware.TaskManagement.Application.Common.Exceptions;
using Anyware.TaskManagement.Application.Common.Interfaces;
using Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.DTOs;
using Anyware.TaskManagement.Domain.Entities;
using Anyware.TaskManagement.Domain.Interfaces;
using Anyware.TaskManagement.Domain.Interfaces.Repositories;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Features.Tasks.Commands.CreateTask
{
    internal sealed class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskDto>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly ITaskQueue _taskQueue;
        private readonly IMapper _mapper;

        public CreateTaskCommandHandler(
            ITaskRepository taskRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            ITaskQueue taskQueue,
            IMapper mapper)
        {
            _taskRepository = taskRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _taskQueue = taskQueue;
            _mapper = mapper;
        }

        public async Task<TaskDto> Handle(
            CreateTaskCommand request,
            CancellationToken cancellationToken)
        {
            var isDuplicate = await _taskRepository.ExistsByTitleAndUserAndDateAsync(
                request.Title,
                _currentUser.UserId,
                DateTime.UtcNow.Date,
                cancellationToken);

            if (isDuplicate)
                throw new ConflictException(
                    $"You already have a task titled '{request.Title}' created today. " +
                    "Duplicate task titles are not allowed within the same day.");

            var task = TaskItem.Create(
                request.Title,
                request.Description,
                request.Priority,
                _currentUser.UserId);

            await _taskRepository.AddAsync(task, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _taskQueue.Enqueue(task.Id);

            return _mapper.Map<TaskDto>(task);
        }
    }
}
