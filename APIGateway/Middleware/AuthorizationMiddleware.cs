namespace APIGateway.Middleware;

public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public AuthorizationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Authorization is handled by ASP.NET Core authorization middleware
        // This middleware can add custom authorization logic

        await _next(context);
    }
}