using Anyware.TaskManagement.Application.Common.Exceptions;
using Anyware.TaskManagement.Application.Common.Interfaces;
using Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.DTOs;
using Anyware.TaskManagement.Domain.Entities;
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
    internal sealed class CreateTaskCommandHandler(
       ITaskRepository taskRepository,
       IUnitOfWork unitOfWork,
       ICurrentUserService currentUser,
       ITaskQueue taskQueue,
       IMapper mapper)
       : IRequestHandler<CreateTaskCommand, TaskDto>
    {
        public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken ct)
        {
            bool duplicate = await taskRepository.ExistsByTitleAndUserAndDateAsync(
                request.Title, currentUser.UserId, DateTime.UtcNow.Date, ct);

            if (duplicate)
                throw new ConflictException(
                    $"A task with title '{request.Title}' already exists for today.");

            var task = TaskItem.Create(request.Title, request.Description, request.Priority, currentUser.UserId);
            await taskRepository.AddAsync(task, ct);
            await unitOfWork.SaveChangesAsync(ct);

            taskQueue.Enqueue(task.Id);

            return mapper.Map<TaskDto>(task);
        }
    }
}
