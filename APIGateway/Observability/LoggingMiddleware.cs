namespace APIGateway.Observability;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = correlationId;

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Request started: {Method} {Path} CorrelationId: {CorrelationId}",
            context.Request.Method, context.Request.Path, correlationId);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            _logger.LogInformation("Request completed: {Method} {Path} Status: {StatusCode} Duration: {Duration}ms CorrelationId: {CorrelationId}",
                context.Request.Method, context.Request.Path, context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds, correlationId);
        }
    }
}

public class MetricsCollector
{
    private static readonly Meter Meter = new("APIGateway.Metrics");
    private static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>("requests_total");
    private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>("request_duration_seconds");

    public static void RecordRequest(string endpoint, string method, int statusCode, double duration)
    {
        RequestCounter.Add(1,
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("status_code", statusCode));

        RequestDuration.Record(duration,
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("method", method));
    }
}