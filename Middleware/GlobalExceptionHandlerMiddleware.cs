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
            var response = new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An error occurred while processing your request."
            };

            // Handle specific exception types
            switch (exception)
            {
                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Message = "You are not authorized to access this resource.";
                    break;

                case KeyNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = "The requested resource was not found.";
                    break;

                case ArgumentException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Invalid request parameters.";
                    break;

                case InvalidOperationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "The operation could not be completed.";
                    break;
            }

            if (_env.IsDevelopment())
            {
                response.DeveloperMessage = exception.Message;
                response.StackTrace = exception.StackTrace;
            }

            context.Response.StatusCode = response.StatusCode;

            // Decide whether to render HTML or JSON. Browsers send
            // "Accept: text/html,...", AJAX/API clients typically send
            // "application/json" or "*/*" without text/html. Render HTML
            // to humans, JSON to machines.
            var accept = context.Request.Headers.Accept.ToString();
            var wantsHtml = accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);

            if (wantsHtml)
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(BuildHtml(response));
            }
            else
            {
                context.Response.ContentType = "application/json";
                var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await context.Response.WriteAsync(jsonResponse);
            }
        }

        private static string BuildHtml(ErrorResponse r)
        {
            // Tiny inline HTML so we don't need a Razor view (we're past the
            // MVC pipeline by the time this runs). Keep it minimal but on-brand.
            var dev = "";
            if (!string.IsNullOrEmpty(r.DeveloperMessage))
            {
                var msg = System.Net.WebUtility.HtmlEncode(r.DeveloperMessage);
                var trace = System.Net.WebUtility.HtmlEncode(r.StackTrace ?? "");
                dev = $@"
                    <details style='margin-top:24px; text-align:left;'>
                        <summary style='cursor:pointer; color:#6b7280;'>Developer details</summary>
                        <pre style='background:#f3f4f6; padding:12px; border-radius:6px; overflow:auto; font-size:12px;'>{msg}

{trace}</pre>
                    </details>";
            }

            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='utf-8' />
    <title>{r.StatusCode} — ShumoShop</title>
    <style>
        body {{ font-family: -apple-system, 'Segoe UI', Roboto, Arial, sans-serif;
                 background: #f3f4f6; color: #1f2937; margin: 0; padding: 40px 20px; }}
        .card {{ max-width: 600px; margin: 60px auto; background: #fff; padding: 40px;
                 border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,.08); text-align: center; }}
        h1 {{ color: #0b66c3; font-size: 64px; margin: 0; }}
        h2 {{ font-size: 20px; margin: 12px 0 16px; }}
        p  {{ color: #6b7280; line-height: 1.6; }}
        a.btn {{ display: inline-block; margin-top: 20px; background: #0b66c3; color: #fff;
                 text-decoration: none; padding: 10px 24px; border-radius: 6px; }}
    </style>
</head>
<body>
    <div class='card'>
        <h1>{r.StatusCode}</h1>
        <h2>{System.Net.WebUtility.HtmlEncode(r.Message)}</h2>
        <p>Sorry — something went wrong. If this keeps happening, please contact support.</p>
        <a class='btn' href='/'>Back to home</a>
        {dev}
    </div>
</body>
</html>";
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