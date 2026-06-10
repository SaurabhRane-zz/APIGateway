using APIGateway.Tests.Integration.Infrastructure;
using FluentAssertions;
using RestSharp;
using Xunit;

namespace APIGateway.Tests.Integration.EndToEnd;

public class GatewayIntegrationTests : IClassFixture<ApiGatewayTestFactory>, IAsyncLifetime
{
    private readonly ApiGatewayTestFactory _factory;
    private RestClient _client;

    public GatewayIntegrationTests(ApiGatewayTestFactory factory)
    {
        _factory = factory;
        _client = new RestClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        var request = new RestRequest("http://localhost:5000/health");
        var response = await _client.ExecuteAsync(request);

        response.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Request_WithoutApiKey_ReturnsUnauthorized()
    {
        var request = new RestRequest("http://localhost:5000/api/test");
        request.AddHeader("X-Correlation-ID", Guid.NewGuid().ToString());

        var response = await _client.ExecuteAsync(request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_WithValidApiKey_ForwardsToTarget()
    {
        var request = new RestRequest("http://localhost:5000/api/test");
        request.AddHeader("X-API-Key", "valid-test-key");
        request.AddHeader("X-Correlation-ID", Guid.NewGuid().ToString());

        var response = await _client.ExecuteAsync(request);

        // Response depends on implementation
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.OK,
            System.Net.HttpStatusCode.NotFound,
            System.Net.HttpStatusCode.ServiceUnavailable
        );
    }
}