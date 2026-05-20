using Microsoft.OpenApi.Models;

namespace Anyware.TaskManagement.API.Extensions;

public static class SwaggerExtensions
{
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddSwaggerWithJwt(
        this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title       = "Anyware Task Management API",
                Version     = "v1",
                Description = "RESTful Task Management API built with Clean Architecture and DDD. " +
                              "Register or log in, copy the access token, click Authorize, " +
                              "and enter: Bearer {token}",
                Contact = new OpenApiContact
                {
                    Name  = "Anyware Software",
                    Email = "dev@anyware.com"
                }
            });

            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Name         = "Authorization",
                Description  = "Enter your JWT token in the format: **Bearer {token}**",
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT",
                In           = ParameterLocation.Header
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id   = BearerScheme
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerWithUi(
        this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Anyware Task Management v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Anyware Task Management API";
            options.DisplayRequestDuration();
            options.DefaultModelsExpandDepth(-1);
        });

        return app;
    }
}
