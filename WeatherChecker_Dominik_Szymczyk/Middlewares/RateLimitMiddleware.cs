using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace WeatherChecker_Dominik_Szymczyk.Middlewares
{
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ConcurrentDictionary<string, List<DateTime>> _ipLog = new();

        private const int LIMIT = 10;
        private static readonly TimeSpan WINDOW = TimeSpan.FromSeconds(30);

        public RateLimitMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            if (ip is null)
            {
                await _next(context);
                return;
            }

            var now = DateTime.UtcNow;
            var timestamps = _ipLog.GetOrAdd(ip, _ => new List<DateTime>());

            var shouldBlock = false;

            lock (timestamps)
            {
                timestamps.RemoveAll(t => t < now - WINDOW);
                if (timestamps.Count >= LIMIT)
                {
                    shouldBlock = true;
                }
                else
                {
                    timestamps.Add(now);
                }
            }

            if (shouldBlock)
            {
                context.Response.StatusCode = 429;
                context.Response.ContentType = "text/plain";
                context.Response.Headers["Retry-After"] = "30";
                await context.Response.WriteAsync("Zbyt wiele żądań. Spróbuj ponownie za 30 sekund.");
                return;
            }

            await _next(context);
        }

    }
}
