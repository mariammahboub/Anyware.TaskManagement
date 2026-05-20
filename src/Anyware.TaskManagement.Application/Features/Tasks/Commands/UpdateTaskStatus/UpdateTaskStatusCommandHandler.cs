using Anyware.TaskManagement.Application.Common.Exceptions;
using Anyware.TaskManagement.Application.Common.Interfaces;
using Anyware.TaskManagement.Domain.Entities;
using Anyware.TaskManagement.Domain.Interfaces;
using Anyware.TaskManagement.Domain.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Features.Tasks.Commands.UpdateTaskStatus
{
    internal sealed class UpdateTaskStatusCommandHandler
        : IRequestHandler<UpdateTaskStatusCommand>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly ICacheService _cache;

        public UpdateTaskStatusCommandHandler(
            ITaskRepository taskRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            ICacheService cache)
        {
            _taskRepository = taskRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _cache = cache;
        }

        public async Task Handle(
            UpdateTaskStatusCommand request,
            CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(
                request.TaskId, cancellationToken)
                ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);
            if (!_currentUser.IsAdmin && !task.BelongsTo(_currentUser.UserId))
                throw new ForbiddenException(
                    "You do not have permission to update this task.");
            task.UpdateStatus(request.NewStatus);

            _taskRepository.Update(task);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _cache.RemoveAsync(CacheKeys.Task(request.TaskId), cancellationToken);
        }
    }
}
