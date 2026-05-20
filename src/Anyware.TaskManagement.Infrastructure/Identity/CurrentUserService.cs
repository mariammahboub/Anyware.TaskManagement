using Anyware.TaskManagement.Application.Common.Exceptions;
using Anyware.TaskManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Infrastructure.Identity
{
    internal sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
            => _httpContextAccessor = httpContextAccessor;

        private ClaimsPrincipal? Principal
            => _httpContextAccessor.HttpContext?.User;
        public Guid UserId
        {
            get
            {
                var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var id))
                    throw new UnauthorizedException(
                        "No valid user identity found on the current request.");

                return id;
            }
        }
        public string Email
            => Principal?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        public bool IsAdmin
            => Principal?.IsInRole("Admin") ?? false;
        public bool IsAuthenticated
            => Principal?.Identity?.IsAuthenticated ?? false;
    }
}
