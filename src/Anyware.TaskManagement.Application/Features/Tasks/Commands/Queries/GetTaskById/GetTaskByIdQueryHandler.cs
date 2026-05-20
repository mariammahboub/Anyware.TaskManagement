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
    internal sealed class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, TaskDto>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ICacheService _cache;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;

        public GetTaskByIdQueryHandler(
            ITaskRepository taskRepository,
            ICacheService cache,
            ICurrentUserService currentUser,
            IMapper mapper)
        {
            _taskRepository = taskRepository;
            _cache = cache;
            _currentUser = currentUser;
            _mapper = mapper;
        }

        public async Task<TaskDto> Handle(
            GetTaskByIdQuery request,
            CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.Task(request.TaskId);
            var cached = await _cache.GetAsync<TaskDto>(cacheKey, cancellationToken);

            if (cached is not null)
            {
                EnforceOwnership(cached);
                return cached;
            }

            var task = await _taskRepository.GetByIdAsync(
                request.TaskId, cancellationToken)
                ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

            var dto = _mapper.Map<TaskDto>(task);
            EnforceOwnership(dto);
            await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10), cancellationToken);

            return dto;
        }

        private void EnforceOwnership(TaskDto dto)
        {
            if (!_currentUser.IsAdmin && dto.UserId != _currentUser.UserId)
                throw new ForbiddenException(
                    "You do not have permission to view this task.");
        }

    }
}