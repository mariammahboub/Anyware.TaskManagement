using Anyware.TaskManagement.Application.Common.Exceptions;
using Anyware.TaskManagement.Application.Common.Models;
using System.Net;
using System.Text.Json;

namespace Anyware.TaskManagement.API.Middleware
{
    public sealed class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, message, errors) = MapException(exception);

            if (statusCode >= 500)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception on {Method} {Path}. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);
            }
            else
            {
                _logger.LogWarning(
                    "{StatusCode} {ExceptionType} on {Method} {Path}: {Message}",
                    statusCode,
                    exception.GetType().Name,
                    context.Request.Method,
                    context.Request.Path,
                    message);
            }

            var response = new ErrorResponse
            {
                StatusCode = statusCode,
                Message = message,
                Errors = errors,
                TraceId = context.TraceIdentifier
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response, JsonOptions));
        }

        private static (int statusCode, string message, IDictionary<string, string[]>? errors)
            MapException(Exception exception)
            => exception switch
            {
                FluentValidation.ValidationException fluentEx =>
                    ((int)HttpStatusCode.UnprocessableEntity,
                     "One or more validation errors occurred.",
                     fluentEx.Errors
                         .GroupBy(e => e.PropertyName)
                         .ToDictionary(
                             g => g.Key,
                             g => g.Select(e => e.ErrorMessage).ToArray())),

                ValidationException ex =>
                    ((int)HttpStatusCode.UnprocessableEntity,
                     "One or more validation errors occurred.",
                     ex.Errors),

                NotFoundException ex =>
                    ((int)HttpStatusCode.NotFound,
                     ex.Message,
                     null),

                UnauthorizedException ex =>
                    ((int)HttpStatusCode.Unauthorized,
                     ex.Message,
                     null),

                ForbiddenException ex =>
                    ((int)HttpStatusCode.Forbidden,
                     ex.Message,
                     null),

                ConflictException ex =>
                    ((int)HttpStatusCode.Conflict,
                     ex.Message,
                     null),

                InvalidOperationException ex =>
                    ((int)HttpStatusCode.BadRequest,
                     ex.Message,
                     null),

                _ =>
                    ((int)HttpStatusCode.InternalServerError,
                     "An unexpected error occurred. Please try again later.",
                     null)
            };
    }
}