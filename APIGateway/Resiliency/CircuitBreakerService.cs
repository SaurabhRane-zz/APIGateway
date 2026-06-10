using Polly;
using Polly.CircuitBreaker;

namespace APIGateway.Resiliency;

public interface ICircuitBreakerService
{
    IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy();
    IAsyncPolicy<HttpResponseMessage> GetRetryPolicy();
    IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy();
}

public class CircuitBreakerService : ICircuitBreakerService
{
    private readonly IConfiguration _configuration;
    private IAsyncPolicy<HttpResponseMessage>? _circuitBreakerPolicy;
    private IAsyncPolicy<HttpResponseMessage>? _retryPolicy;
    private IAsyncPolicy<HttpResponseMessage>? _timeoutPolicy;

    public CircuitBreakerService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        if (_circuitBreakerPolicy != null) return _circuitBreakerPolicy;

        var failureThreshold = _configuration.GetValue<int>("CircuitBreaker:FailureThreshold", 5);
        var samplingDuration = _configuration.GetValue<TimeSpan>("CircuitBreaker:SamplingDuration", TimeSpan.FromSeconds(30));
        var minimumThroughput = _configuration.GetValue<int>("CircuitBreaker:MinimumThroughput", 10);
        var breakDuration = _configuration.GetValue<TimeSpan>("CircuitBreaker:BreakDuration", TimeSpan.FromSeconds(30));

        _circuitBreakerPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => !r.IsSuccessStatusCode)
            .AdvancedCircuitBreakerAsync(
                failureThreshold: (double)failureThreshold / 100,
                samplingDuration: samplingDuration,
                minimumThroughput: minimumThroughput,
                durationOfBreak: breakDuration,
                onBreak: (outcome, timespan) =>
                {
                    Console.WriteLine($"Circuit breaker opened for {timespan.TotalSeconds} seconds");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset");
                },
                onHalfOpen: () =>
                {
                    Console.WriteLine("Circuit breaker half-open");
                });

        return _circuitBreakerPolicy;
    }

    public IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        if (_retryPolicy != null) return _retryPolicy;

        _retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => (int)r.StatusCode >= 500)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds} seconds");
                });

        return _retryPolicy;
    }

    public IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        if (_timeoutPolicy != null) return _timeoutPolicy;

        _timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));
        return _timeoutPolicy;
    }
}