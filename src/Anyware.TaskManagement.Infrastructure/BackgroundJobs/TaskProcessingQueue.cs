using Anyware.TaskManagement.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Infrastructure.BackgroundJobs
{
    internal sealed class TaskProcessingQueue : ITaskQueue
    {
        private readonly Channel<Guid> _channel =
            Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
            {
                SingleReader = true,  
                SingleWriter = false  
            });

        public void Enqueue(Guid taskId)
        {
            if (!_channel.Writer.TryWrite(taskId))
                throw new InvalidOperationException(
                    $"Failed to enqueue task {taskId}. The channel may be completed.");
        }
        public async Task<Guid> DequeueAsync(CancellationToken cancellationToken)
            => await _channel.Reader.ReadAsync(cancellationToken);
    }
}
