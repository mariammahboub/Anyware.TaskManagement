using Anyware.TaskManagement.Domain.Entities;
using Anyware.TaskManagement.Domain.Interfaces.Repositories;
using Anyware.TaskManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Infrastructure.Configurations.Repositories
{
    internal sealed class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
            => _context = context;

        public async Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
            => await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

      
        public async Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
            => await _context.Users
                .FirstOrDefaultAsync(
                    u => u.Email == email.Trim().ToLowerInvariant(),
                    cancellationToken);
        public async Task<IReadOnlyList<User>> GetAllAsync(
            CancellationToken cancellationToken = default)
            => await _context.Users
                .OrderBy(u => u.Name)
                .ToListAsync(cancellationToken);
        public async Task<bool> ExistsByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
            => await _context.Users
                .AnyAsync(
                    u => u.Email == email.Trim().ToLowerInvariant(),
                    cancellationToken);
        public async Task AddAsync(
            User user,
            CancellationToken cancellationToken = default)
            => await _context.Users.AddAsync(user, cancellationToken);
        public void Update(User user)
            => _context.Users.Update(user);
        public void HardDelete(User user)
            => _context.Users.Remove(user);
    }
}
