using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Features.Users.Queries.DTOs
{
    public sealed record UserDto(
     Guid Id,
     string Name,
     string Email,
     string Role,
     DateTime CreatedAt,
     DateTime? UpdatedAt
 );
}
