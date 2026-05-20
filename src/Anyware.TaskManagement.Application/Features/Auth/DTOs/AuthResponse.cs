using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Features.Auth.DTOs
{
    public sealed record AuthResponse(
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiry,
        Guid UserId,
        string Name,
        string Email,
        string Role
    );
}
