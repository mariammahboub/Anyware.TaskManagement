using Anyware.TaskManagement.Domain.Enums;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Features.Tasks.Commands.UpdateTaskStatus
{
    public sealed class UpdateTaskStatusCommandValidator
        : AbstractValidator<UpdateTaskStatusCommand>
    {
        public UpdateTaskStatusCommandValidator()
        {
            RuleFor(x => x.TaskId)
                .NotEmpty().WithMessage("TaskId is required.");

            RuleFor(x => x.NewStatus)
                .IsInEnum().WithMessage(
                    $"Status must be one of: {string.Join(", ", Enum.GetNames<TaskItemStatus>())}.");
        }
    }
}
