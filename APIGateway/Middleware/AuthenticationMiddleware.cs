namespace APIGateway.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public AuthenticationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for health checks and swagger
        if (IsExcludedPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            // JWT authentication handled by ASP.NET Core middleware
        }
        else if (!string.IsNullOrEmpty(apiKey))
        {
            // API Key authentication handled by ASP.NET Core middleware
        }
        else if (_configuration.IsEnabled("EnableWindowsAuthentication"))
        {
            // Windows authentication handled by ASP.NET Core middleware
        }

        await _next(context);
    }

    private bool IsExcludedPath(PathString path)
    {
        var excludedPaths = new[] { "/health", "/swagger", "/metrics", "/favicon.ico" };
        return excludedPaths.Any(p => path.StartsWithSegments(p));
    }
}