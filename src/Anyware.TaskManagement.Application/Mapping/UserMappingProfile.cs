using Anyware.TaskManagement.Application.Features.Users.Queries.DTOs;
using Anyware.TaskManagement.Domain.Entities;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Mapping
{
    public sealed class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<User, UserDto>()
                .ConstructUsing(u => new UserDto(
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Role.ToString(),
                    u.CreatedAt,
                    u.UpdatedAt));
        }
    }
}
