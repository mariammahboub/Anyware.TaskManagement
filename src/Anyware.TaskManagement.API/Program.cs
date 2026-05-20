
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Anyware.TaskManagement.API.Extensions;
using Anyware.TaskManagement.API.Middleware;
using Anyware.TaskManagement.Application;
using Anyware.TaskManagement.Infrastructure;
using Anyware.TaskManagement.Infrastructure.Logging;
using Anyware.TaskManagement.Infrastructure.Persistence;
using Anyware.TaskManagement.Infrastructure.Configurations.Seeders;


Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Anyware Task Management API...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog(SerilogConfigurator.Configure());

    builder.Services.AddApplication();

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            options.JsonSerializerOptions.DefaultIgnoreCondition =
                JsonIgnoreCondition.WhenWritingNull;

            options.JsonSerializerOptions.PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase;
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerWithJwt();

    builder.Services.AddJwtAuthentication(builder.Configuration);

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader());
    });
    builder.Services.AddHealthChecks();

    var app = builder.Build();
    await ApplyDatabaseMigrationsAndSeedAsync(app);

    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseSwaggerWithUi();

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHealthChecks("/health");
    app.MapControllers();

    Log.Information("Anyware Task Management API is ready.");

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup.");
}
finally
{
    await Log.CloseAndFlushAsync();
}
static async Task ApplyDatabaseMigrationsAndSeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");

        logger.LogInformation("Running database seeders...");
        await AdminSeeder.SeedAsync(db, config, logger);
        logger.LogInformation("Database seeding completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex,
            "A fatal error occurred while applying migrations or seeding the database.");
        throw;
    }
}

public partial class Program { }