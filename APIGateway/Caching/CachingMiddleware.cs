using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace APIGateway.Caching;

public class CachingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CachingMiddleware> _logger;

    public CachingMiddleware(
        RequestDelegate next,
        IDistributedCache cache,
        IConfiguration configuration,
        ILogger<CachingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_configuration.IsEnabled("EnableCaching") || !IsCacheableRequest(context))
        {
            await _next(context);
            return;
        }

        var cacheKey = GenerateCacheKey(context);
        var cachedResponse = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedResponse))
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(cachedResponse);
            return;
        }

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        responseBody.Seek(0, SeekOrigin.Begin);
        var responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();

        if (context.Response.StatusCode == 200)
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(cacheKey, responseBodyText, cacheOptions);
        }

        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
    }

    private bool IsCacheableRequest(HttpContext context)
    {
        return context.Request.Method == "GET" &&
               !context.Request.Path.StartsWithSegments("/api/auth");
    }

    private string GenerateCacheKey(HttpContext context)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.Append(context.Request.Path);

        foreach (var query in context.Request.Query.OrderBy(q => q.Key))
        {
            keyBuilder.Append($"|{query.Key}={query.Value}");
        }

        return $"cache:{Convert.ToBase64String(Encoding.UTF8.GetBytes(keyBuilder.ToString()))}";
    }
}