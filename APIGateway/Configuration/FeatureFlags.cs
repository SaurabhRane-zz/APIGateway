namespace APIGateway.Configuration;

public class FeatureFlags
{
    public bool EnableJwtAuthentication { get; set; } = true;
    public bool EnableApiKeyAuthentication { get; set; } = true;
    public bool EnableWindowsAuthentication { get; set; } = false;
    public bool EnableRateLimiting { get; set; } = true;
    public bool EnableCircuitBreaker { get; set; } = true;
    public bool EnableCaching { get; set; } = true;
    public bool EnableServiceDiscovery { get; set; } = true;
    public bool EnableOpenTelemetry { get; set; } = true;
    public bool EnableApiVersioning { get; set; } = true;
    public bool EnableDeveloperPortal { get; set; } = true;
    public bool EnableMessageSecurity { get; set; } = true;
    public bool EnableGovernance { get; set; } = true;
    public bool EnableCloudNative { get; set; } = true;
}

public static class FeatureFlagExtensions
{
    public static bool IsEnabled(this IConfiguration configuration, string flagName)
    {
        return configuration.GetValue<bool>($"FeatureFlags:{flagName}", false);
    }

    public static T GetFeatureConfig<T>(this IConfiguration configuration, string section) where T : class, new()
    {
        return configuration.GetSection(section).Get<T>() ?? new T();
    }
}