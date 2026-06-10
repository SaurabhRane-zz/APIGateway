using APIGateway.Authentication;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace APIGateway.Tests.Unit.Authentication;

public class ApiKeyValidatorTests
{
    [Fact]
    public void Validate_ValidApiKey_ReturnsTrue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"ApiKeys:test-key", "valid-api-key"}
            })
            .Build();

        var validator = new ApiKeyValidator(config);

        var result = validator.Validate("valid-api-key");

        Assert.True(result);
    }

    [Fact]
    public void Validate_InvalidApiKey_ReturnsFalse()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>())
            .Build();

        var validator = new ApiKeyValidator(config);

        var result = validator.Validate("invalid-api-key");

        Assert.False(result);
    }
}