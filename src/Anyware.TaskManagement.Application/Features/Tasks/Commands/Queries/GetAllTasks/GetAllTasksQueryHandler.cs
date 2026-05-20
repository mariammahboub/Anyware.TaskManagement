using Anyware.TaskManagement.Application.Common.Interfaces;
using Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.DTOs;
using Anyware.TaskManagement.Domain.Interfaces.Repositories;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.GetAllTasks
{
    internal sealed class GetAllTasksQueryHandler
       : IRequestHandler<GetAllTasksQuery, IReadOnlyList<TaskDto>>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;

        public GetAllTasksQueryHandler(
            ITaskRepository taskRepository,
            ICurrentUserService currentUser,
            IMapper mapper)
        {
            _taskRepository = taskRepository;
            _currentUser = currentUser;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<TaskDto>> Handle(
            GetAllTasksQuery request,
            CancellationToken cancellationToken)
        {
            var tasks = await _taskRepository.GetAllByUserIdAsync(
                _currentUser.UserId, cancellationToken);
            return tasks
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .Select(_mapper.Map<TaskDto>)
                .ToList()
                .AsReadOnly();
        }
    }
}