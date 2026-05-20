using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Features.Tasks.Commands.Queries.DTOs
{
    public sealed record TaskDto(
        Guid Id,
        string Title,
        string Description,
        string Status,
        string Priority,
        Guid UserId,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
}
