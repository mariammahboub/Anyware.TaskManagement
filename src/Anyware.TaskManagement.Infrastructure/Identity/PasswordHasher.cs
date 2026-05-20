using Anyware.TaskManagement.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Infrastructure.Identity
{
    internal sealed class PasswordHasher : IPasswordHasher
    {
        private const int WorkFactor = 12;
        public string Hash(string password)
            => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        public bool Verify(string password, string hash)
            => BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
