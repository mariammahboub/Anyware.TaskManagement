using Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.GetTaskById
{
    public sealed record GetTaskByIdQuery(Guid TaskId) : IRequest<TaskDto>;

}
