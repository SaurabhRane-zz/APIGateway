using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.Consul;
using Testcontainers.Redis;

namespace APIGateway.Tests.Integration.Infrastructure;

public class ApiGatewayTestFactory : WebApplicationFactory<Program>
{
    private readonly RedisContainer _redisContainer;
    private readonly ConsulContainer _consulContainer;

    public ApiGatewayTestFactory()
    {
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        _consulContainer = new ConsulBuilder()
            .WithImage("hashicorp/consul:latest")
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug);
            });
        });

        builder.UseSetting("ConnectionStrings:Redis", _redisContainer.GetConnectionString());
        builder.UseSetting("Consul:Address", $"http://{_consulContainer.Hostname}:{_consulContainer.GetMappedPublicPort(8500)}");
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        await _consulContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _redisContainer.DisposeAsync();
        await _consulContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}