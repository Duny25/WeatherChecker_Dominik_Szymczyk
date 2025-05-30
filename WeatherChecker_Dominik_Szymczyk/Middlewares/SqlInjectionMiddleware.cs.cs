using System.Text.RegularExpressions;

namespace WeatherChecker_Dominik_Szymczyk.Middlewares
{
    public class SqlInjectionMiddleware
    {
        private readonly RequestDelegate _next;

        public SqlInjectionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var body = string.Empty;

            // Odczytaj body (jeśli istnieje)
            context.Request.EnableBuffering();
            using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            var rawQuery = context.Request.QueryString.ToString();

            if (IsSqlInjection(body) || IsSqlInjection(rawQuery))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Zablokowano potencjalny atak SQL Injection.");
                return;
            }

            await _next(context);
        }

        private bool IsSqlInjection(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;

            // Prosty wzorzec – można rozbudować
            var pattern = @"('|--|;|/\*|\*/|drop|select|insert|update|delete|xp_)";
            return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
        }
    }

    public static class SqlInjectionMiddlewareExtensions
    {
        public static IApplicationBuilder UseSqlInjectionProtection(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SqlInjectionMiddleware>();
        }
    }
}
