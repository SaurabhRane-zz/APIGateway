using Microsoft.Extensions.Caching.Memory;

namespace APIGateway.Authentication;

public interface IApiKeyValidator
{
    Task<bool> ValidateAsync(string apiKey);
    Task<string?> GetClientIdAsync(string apiKey);
}

public class ApiKeyValidator : IApiKeyValidator
{
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly Dictionary<string, string> _apiKeys;

    public ApiKeyValidator(IConfiguration configuration, IMemoryCache cache)
    {
        _configuration = configuration;
        _cache = cache;
        _apiKeys = configuration.GetSection("ApiKey:Keys").Get<Dictionary<string, string>>()
                   ?? new Dictionary<string, string>();
    }

    public Task<bool> ValidateAsync(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return Task.FromResult(false);

        var cacheKey = $"apikey:{apiKey}";
        if (_cache.TryGetValue<bool>(cacheKey, out var isValid))
        {
            return Task.FromResult(isValid);
        }

        isValid = _apiKeys.Values.Contains(apiKey);
        _cache.Set(cacheKey, isValid, TimeSpan.FromMinutes(5));

        return Task.FromResult(isValid);
    }

    public Task<string?> GetClientIdAsync(string apiKey)
    {
        var clientId = _apiKeys.FirstOrDefault(x => x.Value == apiKey).Key;
        return Task.FromResult<string?>(clientId);
    }
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IApiKeyValidator _apiKeyValidator;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyValidator apiKeyValidator)
        : base(options, logger, encoder)
    {
        _apiKeyValidator = apiKeyValidator;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeaderValues))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            return AuthenticateResult.Fail("API Key is missing");
        }

        var isValid = await _apiKeyValidator.ValidateAsync(apiKey);
        if (!isValid)
        {
            return AuthenticateResult.Fail("Invalid API Key");
        }

        var clientId = await _apiKeyValidator.GetClientIdAsync(apiKey);
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, clientId ?? "unknown"),
            new Claim("ApiKey", apiKey)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}