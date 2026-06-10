using APIGateway.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace APIGateway.Tests.Unit.Middleware;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AddsCorrelationId_WhenNotPresent()
    {
        var context = new DefaultHttpContext();
        var logger = Mock.Of<ILogger<CorrelationIdMiddleware>>();
        var middleware = new CorrelationIdMiddleware(next: (ctx) => Task.CompletedTask, logger);

        await middleware.InvokeAsync(context);

        Assert.True(context.Response.Headers.ContainsKey("X-Correlation-ID"));
    }

    [Fact]
    public async Task InvokeAsync_UsesExistingCorrelationId_WhenPresent()
    {
        var context = new DefaultHttpContext();
        var existingCorrelationId = "test-correlation-id";
        context.Request.Headers["X-Correlation-ID"] = existingCorrelationId;
        var logger = Mock.Of<ILogger<CorrelationIdMiddleware>>();
        var middleware = new CorrelationIdMiddleware(next: (ctx) => Task.CompletedTask, logger);

        await middleware.InvokeAsync(context);

        Assert.Equal(existingCorrelationId, context.Response.Headers["X-Correlation-ID"]);
    }
}