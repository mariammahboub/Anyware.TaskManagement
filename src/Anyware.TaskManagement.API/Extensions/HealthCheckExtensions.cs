using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Anyware.TaskManagement.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddDependencyHealthChecks(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        var pg    = configuration.GetConnectionString("DefaultConnection");
        var redis = configuration.GetConnectionString("Redis");

        var hc = services.AddHealthChecks();

        if (!string.IsNullOrWhiteSpace(pg))
            hc.AddNpgSql(pg,
                name: "PostgreSQL",
                tags: ["db", "sql", "ready"]);

        if (!string.IsNullOrWhiteSpace(redis))
            hc.AddRedis(redis,
                name: "Redis",
                tags: ["cache", "ready"]);

        return services;
    }

    public static WebApplication UseHealthCheckEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");

        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        return app;
    }
}
