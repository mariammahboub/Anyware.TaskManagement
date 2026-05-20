using Anyware.TaskManagement.Application.Common.Interfaces;
using Anyware.TaskManagement.Domain.Interfaces;
using Anyware.TaskManagement.Domain.Interfaces.Repositories;
using Anyware.TaskManagement.Infrastructure.BackgroundJobs;
using Anyware.TaskManagement.Infrastructure.Caching;
using Anyware.TaskManagement.Infrastructure.Configurations.Repositories;
using Anyware.TaskManagement.Infrastructure.Identity;
using Anyware.TaskManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Infrastructure
{

    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services
                .AddDatabase(configuration)
                .AddRedis(configuration)
                .AddRepositories()
                .AddIdentityServices()
                .AddBackgroundProcessing();

            return services;
        }

        private static IServiceCollection AddDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "DefaultConnection is not configured.");

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(
                    connectionString,
                    npgsql =>
                    {
                        npgsql.MigrationsAssembly(
                            typeof(ApplicationDbContext).Assembly.FullName);
                        npgsql.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorCodesToAdd: null);
                    });

#if DEBUG
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
#endif
            });

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }

        private static IServiceCollection AddRedis(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var redisConnectionString = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException(
                    "Redis connection string is not configured.");

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnectionString));

            services.AddScoped<ICacheService, RedisCacheService>();

            return services;
        }

        private static IServiceCollection AddRepositories(
            this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();

            return services;
        }

        private static IServiceCollection AddIdentityServices(
            this IServiceCollection services)
        {

            services.AddHttpContextAccessor();

            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddSingleton<IJwtService, JwtService>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();

            return services;
        }
        private static IServiceCollection AddBackgroundProcessing(
            this IServiceCollection services)
        {

            services.AddSingleton<ITaskQueue, TaskProcessingQueue>();

            services.AddHostedService<TaskProcessingWorker>();

            return services;
        }
    }
}