using System.Text;
using System.Security.Claims;

namespace WeatherChecker_Dominik_Szymczyk.Logging
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var request = context.Request;
            var method = request.Method;
            var path = request.Path;
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var timestamp = DateTime.UtcNow;

            // Odczytujemy e-mail, jeśli użytkownik jest zalogowany
            var email = context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "anonim";

            // Odczytujemy ciało (body)
            string body = "";
            if (request.ContentLength > 0 && request.Body.CanSeek)
            {
                request.EnableBuffering();
                request.Body.Position = 0;
                using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    body = await reader.ReadToEndAsync();
                    request.Body.Position = 0;
                }
            }

            _logger.LogInformation($"[{timestamp}] {method} {path} | IP: {ip} | Email: {email} | Body: {body}");

            await _next(context);
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
