using Anyware.TaskManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Domain.Events
{
    public sealed record TaskCreatedDomainEvent(Guid TaskId, Guid UserId) : IDomainEvent;
}
