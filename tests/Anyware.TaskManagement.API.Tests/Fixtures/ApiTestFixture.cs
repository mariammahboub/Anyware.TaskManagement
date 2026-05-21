using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Anyware.TaskManagement.Application.Common.Interfaces;
using Anyware.TaskManagement.API.Tests.Infrastructure;
using Anyware.TaskManagement.Infrastructure.Persistence;

namespace Anyware.TaskManagement.API.Tests.Fixtures;

public sealed class ApiTestFixture : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"IntegrationTest_{Guid.NewGuid():N}";
    private InMemoryCacheService? _cache;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test_db;Username=test;Password=test",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["AdminSeed:Name"] = "Anyware Admin",
                ["AdminSeed:Email"] = "admin@anyware.com",
                ["AdminSeed:Password"] = "Admin@123",
                ["Jwt:Key"] = "AnywareSoftware_SuperSecretKey_2024_MustBe32Chars!",
                ["Jwt:Issuer"] = "AnywareTaskManagementAPI",
                ["Jwt:Audience"] = "AnywareClients",
                ["Jwt:ExpiryHours"] = "1",
                ["Jwt:RefreshTokenExpiryDays"] = "7"
            });
        });

        builder.ConfigureServices(services =>
        {
            Remove<DbContextOptions<ApplicationDbContext>>(services);
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            Remove<IConnectionMultiplexer>(services);
            Remove<ICacheService>(services);

            _cache = new InMemoryCacheService();
            services.AddSingleton<ICacheService>(_cache);

            var workerDescriptor = services.FirstOrDefault(d =>
                d.ImplementationType?.Name == "TaskProcessingWorker");
            if (workerDescriptor is not null)
                services.Remove(workerDescriptor);
        });
    }  

    public HttpClient CreateApiClient()
        => CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

    public InMemoryCacheService GetCache() => _cache!;

    private static void Remove<T>(IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor is not null) services.Remove(descriptor);
    }
} 
