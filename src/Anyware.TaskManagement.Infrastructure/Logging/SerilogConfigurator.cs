using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Infrastructure.Logging
{
    public static class SerilogConfigurator
    {
        public static Action<HostBuilderContext, LoggerConfiguration> Configure()
            => (context, config) =>
            {
                var isDevelopment = context.HostingEnvironment.IsDevelopment();

                config
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", "Anyware.TaskManagement")
                    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);

                if (isDevelopment)
                {
                    config.WriteTo.Console(
                        outputTemplate:
                            "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}" +
                            "{Message:lj}{NewLine}{Exception}");

                    config.MinimumLevel.Override(
                        "Microsoft.EntityFrameworkCore.Database.Command",
                        LogEventLevel.Information); 
                }
                else
                {
                    config.WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter());
                }
                config.WriteTo.File(
                    path: "logs/anyware-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] " +
                        "{SourceContext} {Message:lj}{NewLine}{Exception}");
            };
    }
}
