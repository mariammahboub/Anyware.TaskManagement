using Anyware.TaskManagement.Application.Features.Users.Queries.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Features.Users.Queries.GetAllUsers
{
    public sealed record GetAllUsersQuery : IRequest<IReadOnlyList<UserDto>>;

}
