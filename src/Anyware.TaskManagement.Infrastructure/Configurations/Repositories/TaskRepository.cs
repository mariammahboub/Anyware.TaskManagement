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

    internal sealed class TaskRepository : ITaskRepository
    {
        private readonly ApplicationDbContext _context;

        public TaskRepository(ApplicationDbContext context)
            => _context = context;
        public async Task<TaskItem?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
            => await _context.Tasks
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        public async Task<IReadOnlyList<TaskItem>> GetAllByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
            => await _context.Tasks
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Priority) 
                .ThenBy(t => t.CreatedAt)             
                .ToListAsync(cancellationToken);
        public async Task<bool> ExistsByTitleAndUserAndDateAsync(
            string title,
            Guid userId,
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            var startOfDay = date.Date;                
            var startOfNext = startOfDay.AddDays(1); 
            var normalized = title.Trim().ToLowerInvariant();

            return await _context.Tasks
                .AnyAsync(t =>
                    t.UserId == userId
                    && t.Title.ToLower() == normalized
                    && t.CreatedAt >= startOfDay
                    && t.CreatedAt < startOfNext,
                    cancellationToken);
        }

        public async Task AddAsync(
            TaskItem task,
            CancellationToken cancellationToken = default)
            => await _context.Tasks.AddAsync(task, cancellationToken);
        public void Update(TaskItem task)
            => _context.Tasks.Update(task);
    }
}
