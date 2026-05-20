using Anyware.TaskManagement.Domain.Common;
using Anyware.TaskManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Domain.Entities
{
    public sealed class User : BaseEntity
    {
        public string Name { get; private set; } = default!;
        public string Email { get; private set; } = default!;
        public string PasswordHash { get; private set; } = default!;
        public UserRole Role { get; private set; }
        public bool IsDeleted { get; private set; }     

        private User() { }  

        public static User Create(string name, string email, string passwordHash, UserRole role = UserRole.User)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(email);
            return new User { Name = name, Email = email, PasswordHash = passwordHash, Role = role };
        }

        public void SoftDelete() { IsDeleted = true; SetUpdatedAt(); }
        public void UpdateName(string name) { Name = name; SetUpdatedAt(); }
    }
}
