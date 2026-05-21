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
        public string? RefreshToken { get; private set; }
        public DateTime? RefreshTokenExpiry { get; private set; }
        private User() { }
        public static User Create(
            string name,
            string email,
            string passwordHash,
            UserRole role = UserRole.User)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
            ArgumentException.ThrowIfNullOrWhiteSpace(email, nameof(email));
            ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash, nameof(passwordHash));

            return new User
            {
                Name = name.Trim(),
                Email = email.Trim().ToLowerInvariant(),
                PasswordHash = passwordHash,
                Role = role
            };
        }
        public void SetRefreshToken(string refreshToken, int expiryDays)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken, nameof(refreshToken));
            RefreshToken = refreshToken;
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(expiryDays);
            MarkAsUpdated();
        }

        public void ClearRefreshToken()
        {
            RefreshToken = null;
            RefreshTokenExpiry = null;
            MarkAsUpdated();
        }

        public bool IsRefreshTokenValid(string token)
            => RefreshToken == token
               && RefreshTokenExpiry.HasValue
               && RefreshTokenExpiry.Value > DateTime.UtcNow;
        public void UpdateName(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
            Name = name.Trim();
            MarkAsUpdated();
        }
        public void UpdatePasswordHash(string newPasswordHash)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash, nameof(newPasswordHash));
            PasswordHash = newPasswordHash;
            MarkAsUpdated();
        }
        public void SoftDelete()
        {
            IsDeleted = true;
            MarkAsUpdated();
        }
    public void Restore()
        {
            IsDeleted = false;
            MarkAsUpdated();
        }
    } }