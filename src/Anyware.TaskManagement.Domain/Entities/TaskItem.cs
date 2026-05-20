using Anyware.TaskManagement.Domain.Common;
using Anyware.TaskManagement.Domain.Enums;
using Anyware.TaskManagement.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Domain.Entities
{
    public sealed class TaskItem : BaseEntity
    {
        public string Title { get; private set; } = default!;
        public string Description { get; private set; } = default!;
        public TaskItemStatus Status { get; private set; }
        public TaskPriority Priority { get; private set; }
        public Guid UserId { get; private set; }
        public User User { get; private set; } = default!;

        private TaskItem() { }

        public static TaskItem Create(string title, string description, TaskPriority priority, Guid userId)
        {
            var task = new TaskItem
            {
                Title = title,
                Description = description,
                Priority = priority,
                UserId = userId,
                Status = TaskItemStatus.Pending
            };
            task.AddDomainEvent(new TaskCreatedDomainEvent(task.Id, userId));
            return task;
        }

        public void UpdateStatus(TaskItemStatus newStatus) { Status = newStatus; SetUpdatedAt(); }
    }
}
