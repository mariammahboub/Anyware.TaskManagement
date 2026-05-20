using Anyware.TaskManagement.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Features.Tasks.Commands.UpdateTaskStatus
{
    public sealed record UpdateTaskStatusCommand(
      Guid TaskId,
      TaskItemStatus NewStatus
  ) : IRequest;
}
