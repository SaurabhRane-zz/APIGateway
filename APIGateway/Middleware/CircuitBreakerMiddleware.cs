using Yarp.ReverseProxy.Model;

namespace APIGateway.Middleware;

public class CircuitBreakerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICircuitBreakerService _circuitBreakerService;
    private readonly IConfiguration _configuration;

    public CircuitBreakerMiddleware(
        RequestDelegate next,
        ICircuitBreakerService circuitBreakerService,
        IConfiguration configuration)
    {
        _next = next;
        _circuitBreakerService = circuitBreakerService;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_configuration.IsEnabled("EnableCircuitBreaker"))
        {
            await _next(context);
            return;
        }

        var policy = Policy.WrapAsync(
            _circuitBreakerService.GetCircuitBreakerPolicy(),
            _circuitBreakerService.GetRetryPolicy(),
            _circuitBreakerService.GetTimeoutPolicy()
        );

        try
        {
            await policy.ExecuteAsync(async () => await _next(context));
        }
        catch (BrokenCircuitException)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Service temporarily unavailable due to circuit breaker");
        }
        catch (TimeoutRejectedException)
        {
            context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            await context.Response.WriteAsync("Request timeout");
        }
    }
}