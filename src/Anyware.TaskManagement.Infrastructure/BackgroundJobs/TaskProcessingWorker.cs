using Anyware.TaskManagement.Application.Common.Interfaces;
using Anyware.TaskManagement.Domain.Enums;
using Anyware.TaskManagement.Domain.Interfaces;
using Anyware.TaskManagement.Domain.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Infrastructure.BackgroundJobs
{
    internal sealed class TaskProcessingWorker : BackgroundService
    {
        private readonly ITaskQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TaskProcessingWorker> _logger;

        public TaskProcessingWorker(
            ITaskQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<TaskProcessingWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "TaskProcessingWorker started. Listening for tasks...");

            while (!stoppingToken.IsCancellationRequested)
            {
                Guid taskId;

                try
                {
                    taskId = await _queue.DequeueAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("TaskProcessingWorker shutting down.");
                    break;
                }

                await ProcessTaskSafelyAsync(taskId, stoppingToken);
            }
        }

        private async Task ProcessTaskSafelyAsync(
            Guid taskId,
            CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation(
                    "TaskProcessingWorker: starting to process task {TaskId}", taskId);

                await ProcessTaskAsync(taskId, stoppingToken);

                _logger.LogInformation(
                    "TaskProcessingWorker: completed processing task {TaskId}", taskId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex,
                    "TaskProcessingWorker: unhandled error while processing task {TaskId}",
                    taskId);
            }
        }

     

        private async Task ProcessTaskAsync(
            Guid taskId,
            CancellationToken stoppingToken)
        {
  
            using var scope = _scopeFactory.CreateScope();

            var taskRepository = scope.ServiceProvider
                .GetRequiredService<ITaskRepository>();
            var cacheService = scope.ServiceProvider
                .GetRequiredService<ICacheService>();
            var unitOfWork = scope.ServiceProvider
                .GetRequiredService<IUnitOfWork>();

            var task = await taskRepository.GetByIdAsync(taskId, stoppingToken);

            if (task is null)
            {
                _logger.LogWarning(
                    "TaskProcessingWorker: task {TaskId} not found in database — skipping.",
                    taskId);
                return;
            }

            task.UpdateStatus(TaskItemStatus.InProgress);
            taskRepository.Update(task);
            await unitOfWork.SaveChangesAsync(stoppingToken);

            await cacheService.RemoveAsync($"task:{taskId}", stoppingToken);

            _logger.LogInformation(
                "TaskProcessingWorker: task {TaskId} is now InProgress", taskId);

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

 
            var taskForCompletion = await taskRepository.GetByIdAsync(taskId, stoppingToken);

            if (taskForCompletion is null) return; 

            taskForCompletion.UpdateStatus(TaskItemStatus.Done);
            taskRepository.Update(taskForCompletion);
            await unitOfWork.SaveChangesAsync(stoppingToken);
            await cacheService.RemoveAsync($"task:{taskId}", stoppingToken);

            _logger.LogInformation(
                "TaskProcessingWorker: task {TaskId} is now Done", taskId);
        }
    }
}
