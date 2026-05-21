using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Features.Auth.DTOs
{
    public sealed record RefreshTokenRequest(
           string AccessToken,
           string RefreshToken
       );
}