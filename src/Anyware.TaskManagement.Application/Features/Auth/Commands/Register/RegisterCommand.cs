using Anyware.TaskManagement.Application.Features.Auth.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Features.Auth.Commands.Register
{
    public sealed record RegisterCommand(
        string Name,
        string Email,
        string Password
    ) : IRequest<AuthResponse>;
}
