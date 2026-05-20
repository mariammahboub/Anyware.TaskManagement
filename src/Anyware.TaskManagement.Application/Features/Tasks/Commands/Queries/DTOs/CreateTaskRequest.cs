using Anyware.TaskManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.DTOs
{
    public sealed record CreateTaskRequest(
        string Title,
        string Description,
        TaskPriority Priority
    );
}
