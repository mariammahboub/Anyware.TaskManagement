using Anyware.TaskManagement.Domain.Entities;
using Anyware.TaskManagement.Domain.Enums;
using Anyware.TaskManagement.Domain.Events;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Domain.Tests.Entities
{
    public sealed class TaskItemTests
    {
        private static readonly Guid ValidUserId = Guid.NewGuid();
        [Fact]
        public void Create_WithValidParameters_ReturnsPendingTaskWithCorrectProperties()
        {
            var task = TaskItem.Create(
                "Fix login bug", "The login button does not work", TaskPriority.High, ValidUserId);

            task.Title.Should().Be("Fix login bug");
            task.Description.Should().Be("The login button does not work");
            task.Priority.Should().Be(TaskPriority.High);
            task.UserId.Should().Be(ValidUserId);
            task.Status.Should().Be(TaskItemStatus.Pending);
            task.Id.Should().NotBe(Guid.Empty);
            task.IsCompleted.Should().BeFalse();
            task.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            task.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_TitleAndDescriptionAreTrimmed()
        {
            var task = TaskItem.Create(
                "  My Task  ", "  My Description  ", TaskPriority.Low, ValidUserId);

            task.Title.Should().Be("My Task");
            task.Description.Should().Be("My Description");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithBlankTitle_ThrowsArgumentException(string title)
        {
            var act = () => TaskItem.Create(title, "Description", TaskPriority.Low, ValidUserId);
            act.Should().Throw<ArgumentException>().WithParameterName("title");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithBlankDescription_ThrowsArgumentException(string description)
        {
            var act = () => TaskItem.Create("Title", description, TaskPriority.Low, ValidUserId);
            act.Should().Throw<ArgumentException>().WithParameterName("description");
        }

        [Fact]
        public void Create_WithEmptyUserId_ThrowsArgumentException()
        {
            var act = () => TaskItem.Create("Title", "Desc", TaskPriority.Low, Guid.Empty);
            act.Should().Throw<ArgumentException>().WithParameterName("userId");
        }
        [Fact]
        public void Create_RaisesExactlyOneTaskCreatedDomainEvent()
        {
            var task = TaskItem.Create("Title", "Desc", TaskPriority.Medium, ValidUserId);
            task.DomainEvents.Should().HaveCount(1);
            task.DomainEvents.First().Should().BeOfType<TaskCreatedDomainEvent>();
        }

        [Fact]
        public void Create_DomainEvent_ContainsCorrectTaskIdAndUserId()
        {
            var task = TaskItem.Create("Title", "Desc", TaskPriority.Low, ValidUserId);
            var evt = (TaskCreatedDomainEvent)task.DomainEvents.First();

            evt.TaskId.Should().Be(task.Id);
            evt.UserId.Should().Be(ValidUserId);
            evt.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void ClearDomainEvents_RemovesAllEvents()
        {
            var task = TaskItem.Create("Title", "Desc", TaskPriority.Low, ValidUserId);
            task.ClearDomainEvents();
            task.DomainEvents.Should().BeEmpty();
        }
        [Theory]
        [InlineData(TaskItemStatus.Pending, TaskItemStatus.InProgress)]
        [InlineData(TaskItemStatus.Pending, TaskItemStatus.Done)]
        [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Done)]
        [InlineData(TaskItemStatus.Done, TaskItemStatus.Done)]
        public void UpdateStatus_AllowedTransitions_UpdatesStatusAndSetsUpdatedAt(
            TaskItemStatus initial, TaskItemStatus target)
        {
            var task = TaskItem.Create("Title", "Desc", TaskPriority.Low, ValidUserId);

            if (initial != TaskItemStatus.Pending)
                task.UpdateStatus(initial);

            task.UpdateStatus(target);

            task.Status.Should().Be(target);
            task.UpdatedAt.Should().NotBeNull();
        }

        [Theory]
        [InlineData(TaskItemStatus.Pending)]
        [InlineData(TaskItemStatus.InProgress)]
        public void UpdateStatus_FromDoneToNonDone_ThrowsInvalidOperationException(
            TaskItemStatus targetStatus)
        {
            var task = TaskItem.Create("Title", "Desc", TaskPriority.Low, ValidUserId);
            task.UpdateStatus(TaskItemStatus.Done);

            var act = () => task.UpdateStatus(targetStatus);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*completed*");
        }
        [Fact]
        public void BelongsTo_WithMatchingUserId_ReturnsTrue()
        {
            var task = TaskItem.Create("Title", "Desc", TaskPriority.Low, ValidUserId);
            task.BelongsTo(ValidUserId).Should().BeTrue();
        }

        [Fact]
        public void BelongsTo_WithDifferentUserId_ReturnsFalse()
        {
            var task = TaskItem.Create("Title", "Desc", TaskPriority.Low, ValidUserId);
            task.BelongsTo(Guid.NewGuid()).Should().BeFalse();
        }
        [Fact]
        public void IsCompleted_WhenStatusIsDone_ReturnsTrue()
        {
            var task = TaskItem.Create("Title", "Desc", TaskPriority.Low, ValidUserId);
            task.UpdateStatus(TaskItemStatus.Done);
            task.IsCompleted.Should().BeTrue();
        }

        [Theory]
        [InlineData(TaskItemStatus.Pending)]
        [InlineData(TaskItemStatus.InProgress)]
        public void IsCompleted_WhenStatusIsNotDone_ReturnsFalse(TaskItemStatus status)
        {
            var task = TaskItem.Create("Title", "Desc", TaskPriority.Low, ValidUserId);
            if (status == TaskItemStatus.InProgress)
                task.UpdateStatus(TaskItemStatus.InProgress);

            task.IsCompleted.Should().BeFalse();
        }
        [Fact]
        public void UpdatePriority_WithNewPriority_UpdatesPriorityAndSetsUpdatedAt()
        {
            var task = TaskItem.Create("Title", "Desc", TaskPriority.Low, ValidUserId);
            task.UpdatePriority(TaskPriority.Critical);
            task.Priority.Should().Be(TaskPriority.Critical);
            task.UpdatedAt.Should().NotBeNull();
        }
    }
}