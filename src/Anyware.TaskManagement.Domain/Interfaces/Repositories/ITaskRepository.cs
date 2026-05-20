using Anyware.TaskManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Domain.Interfaces.Repositories
{
    public interface ITaskRepository
    {
        Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<TaskItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<bool> ExistsByTitleAndUserAndDateAsync(string title, Guid userId, DateTime date, CancellationToken ct = default);
        Task AddAsync(TaskItem task, CancellationToken ct = default);
        void Update(TaskItem task);
    }
}
