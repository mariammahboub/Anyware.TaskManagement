using Anyware.TaskManagement.Application.Features.Auth.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Features.Auth.Commands.Register
{
    public sealed record RefreshTokenCommand(
       string AccessToken,
       string RefreshToken
   ) : IRequest<AuthResponse>;
}
