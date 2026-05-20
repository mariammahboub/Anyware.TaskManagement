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

namespace Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.GetTaskById
{
    internal sealed class GetTaskByIdQueryHandler(
        ITaskRepository taskRepository,
        ICacheService cache,
        ICurrentUserService currentUser,
        IMapper mapper)
        : IRequestHandler<GetTaskByIdQuery, TaskDto>
    {
        private static string CacheKey(Guid id) => $"task:{id}";

        public async Task<TaskDto> Handle(GetTaskByIdQuery request, CancellationToken ct)
        {
            var cached = await cache.GetAsync<TaskDto>(CacheKey(request.TaskId), ct);
            if (cached is not null) return cached;

            var task = await taskRepository.GetByIdAsync(request.TaskId, ct)
                ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

            if (task.UserId != currentUser.UserId && !currentUser.IsAdmin)
                throw new UnauthorizedException();

            var dto = mapper.Map<TaskDto>(task);
            await cache.SetAsync(CacheKey(request.TaskId), dto, TimeSpan.FromMinutes(10), ct);
            return dto;
        }
    }
}
