using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Common.Interfaces
{
    public interface ITaskQueue
    {
        void Enqueue(Guid taskId);
        Task<Guid> DequeueAsync(CancellationToken ct);
    }
}
