namespace APIGateway.Versioning;

public interface IApiVersioningService
{
    string GetApiVersion(HttpContext context);
    bool IsVersionSupported(string version);
    List<string> GetSupportedVersions();
}

public class ApiVersioningService : IApiVersioningService
{
    private readonly List<string> _supportedVersions = new() { "1.0", "2.0", "3.0" };

    public string GetApiVersion(HttpContext context)
    {
        // Check URL path
        var pathVersion = ExtractVersionFromPath(context.Request.Path);
        if (!string.IsNullOrEmpty(pathVersion)) return pathVersion;

        // Check header
        if (context.Request.Headers.TryGetValue("X-API-Version", out var headerVersion))
        {
            return headerVersion.ToString();
        }

        // Check query parameter
        if (context.Request.Query.TryGetValue("api-version", out var queryVersion))
        {
            return queryVersion.ToString();
        }

        return "1.0"; // Default version
    }

    public bool IsVersionSupported(string version)
    {
        return _supportedVersions.Contains(version);
    }

    public List<string> GetSupportedVersions()
    {
        return _supportedVersions;
    }

    private string? ExtractVersionFromPath(string path)
    {
        var match = System.Text.RegularExpressions.Regex.Match(path, @"/api/v(\d+\.?\d*)/");
        return match.Success ? match.Groups[1].Value : null;
    }
}

public static class ApiVersioningExtensions
{
    public static RouteGroupBuilder MapApiEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
             .WithMetadata(new ApiVersionMetadata("1.0", "2.0", "3.0"));

        group.MapGet("/version", () => Results.Ok(new
        {
            CurrentVersion = "3.0",
            SupportedVersions = new[] { "1.0", "2.0", "3.0" }
        }));

        return group;
    }
}

public class ApiVersionMetadata
{
    public string[] SupportedVersions { get; }

    public ApiVersionMetadata(params string[] versions)
    {
        SupportedVersions = versions;
    }
}