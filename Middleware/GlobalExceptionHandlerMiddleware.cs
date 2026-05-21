using System.Net;
using System.Text.Json;

namespace WebApplication1.Middleware
{
    /// <summary>
    /// Global exception handling middleware
    /// Catches all unhandled exceptions and returns appropriate responses
    /// </summary>
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new ErrorResponse
            {
                StatusCode = context.Response.StatusCode,
                Message = "An error occurred while processing your request."
            };

            // In development, include detailed error information
            if (_env.IsDevelopment())
            {
                response.DeveloperMessage = exception.Message;
                response.StackTrace = exception.StackTrace;
            }

            // Handle specific exception types
            switch (exception)
            {
                case UnauthorizedAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.StatusCode = context.Response.StatusCode;
                    response.Message = "You are not authorized to access this resource.";
                    break;

                case KeyNotFoundException:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.StatusCode = context.Response.StatusCode;
                    response.Message = "The requested resource was not found.";
                    break;

                case ArgumentException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.StatusCode = context.Response.StatusCode;
                    response.Message = "Invalid request parameters.";
                    if (_env.IsDevelopment())
                    {
                        response.DeveloperMessage = exception.Message;
                    }
                    break;

                case InvalidOperationException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.StatusCode = context.Response.StatusCode;
                    response.Message = "The operation could not be completed.";
                    break;

                default:
                    // Generic 500 error
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private class ErrorResponse
        {
            public int StatusCode { get; set; }
            public string Message { get; set; } = string.Empty;
            public string? DeveloperMessage { get; set; }
            public string? StackTrace { get; set; }
        }
    }

    /// <summary>
    /// Extension method to register the global exception handler
    /// </summary>
    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
}