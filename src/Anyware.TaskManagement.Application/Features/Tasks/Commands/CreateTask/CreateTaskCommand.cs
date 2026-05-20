using Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.DTOs;
using Anyware.TaskManagement.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Features.Tasks.Commands.CreateTask
{
    public sealed record CreateTaskCommand(
     string Title,
     string Description,
     TaskPriority Priority
 ) : IRequest<TaskDto>;

}
