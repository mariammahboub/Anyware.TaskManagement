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
        public static TaskItem Create(
            string title,
            string description,
            TaskPriority priority,
            Guid userId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
            ArgumentException.ThrowIfNullOrWhiteSpace(description, nameof(description));
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId must not be empty.", nameof(userId));

            var task = new TaskItem
            {
                Title = title.Trim(),
                Description = description.Trim(),
                Priority = priority,
                UserId = userId,
                Status = TaskItemStatus.Pending
            };
            task.AddDomainEvent(new TaskCreatedDomainEvent(task.Id, userId));

            return task;
        }

        public void UpdateStatus(TaskItemStatus newStatus)
        {
            if (Status == TaskItemStatus.Done && newStatus != TaskItemStatus.Done)
                throw new InvalidOperationException(
                    $"Cannot transition a completed task back to '{newStatus}'.");

            Status = newStatus;
            MarkAsUpdated();
        }
        public void UpdateTitle(string title)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
            Title = title.Trim();
            MarkAsUpdated();
        }

        public void UpdateDescription(string description)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(description, nameof(description));
            Description = description.Trim();
            MarkAsUpdated();
        }
        public void UpdatePriority(TaskPriority priority)
        {
            Priority = priority;
            MarkAsUpdated();
        }

        public bool BelongsTo(Guid userId) => UserId == userId;

        public bool IsCompleted => Status == TaskItemStatus.Done;
    }
}