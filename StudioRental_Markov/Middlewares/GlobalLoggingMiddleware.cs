using Microsoft.AspNetCore.Http;
using StudioRental_Markov.Services;
using System.Text;

namespace StudioRental_Markov.Middlewares
{
    public class GlobalLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalLoggingMiddleware> _logger;

        public GlobalLoggingMiddleware(RequestDelegate next, ILogger<GlobalLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, LoggingService loggingService)
        {
            var startTime = DateTime.Now;

            var importantMethods = new[] { "POST", "PUT", "DELETE" };
            var isImportant = importantMethods.Contains(context.Request.Method);

            string? requestBody = null;

            if (isImportant && (context.Request.ContentType?.Contains("application/json") == true))
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            try
            {
                await _next(context);

                var duration = DateTime.Now - startTime;

                if (duration.TotalSeconds > 3)
                {
                    await loggingService.LogWarningAsync(
                        "Performance",
                        context.Request.Path,
                        $"Медленный запрос: {duration.TotalMilliseconds:F0}ms",
                        $"Path: {context.Request.Path}, Method: {context.Request.Method}"
                    );
                }
            }
            catch (Exception ex)
            {
                await loggingService.LogErrorWithException(
                    "System",
                    $"{context.Request.Method} {context.Request.Path}",
                    ex,
                    $"RequestBody: {requestBody}"
                );
                throw;
            }
        }
    }
}