using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Yarp.ReverseProxy.Configuration;
using AspNetCoreRateLimit;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                    .AddEnvironmentVariables();

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Feature Flags Configuration
var featureFlags = builder.Configuration.GetSection("FeatureFlags");
bool enableJwtAuth = featureFlags.GetValue<bool>("EnableJwtAuthentication");
bool enableApiKeyAuth = featureFlags.GetValue<bool>("EnableApiKeyAuthentication");
bool enableWindowsAuth = featureFlags.GetValue<bool>("EnableWindowsAuthentication");
bool enableRateLimiting = featureFlags.GetValue<bool>("EnableRateLimiting");
bool enableCircuitBreaker = featureFlags.GetValue<bool>("EnableCircuitBreaker");
bool enableCaching = featureFlags.GetValue<bool>("EnableCaching");
bool enableServiceDiscovery = featureFlags.GetValue<bool>("EnableServiceDiscovery");
bool enableOpenTelemetry = featureFlags.GetValue<bool>("EnableOpenTelemetry");

// Authentication
var authBuilder = builder.Services.AddAuthentication();

if (enableJwtAuth)
{
    authBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "default-secret-key"))
        };
    });
}

if (enableWindowsAuth)
{
    authBuilder.AddNegotiate();
}

if (enableApiKeyAuth)
{
    builder.Services.AddSingleton<IApiKeyValidator, ApiKeyValidator>();
}

// Authorization
builder.Services.AddAuthorization(options =>
{
    // RBAC Policies
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));

    // Claims-based Policies
    options.AddPolicy("RequireEmail", policy => policy.RequireClaim("email"));
    options.AddPolicy("RequireDepartment", policy => policy.RequireClaim("department", "IT", "HR", "Finance"));

    // Policy-based
    options.AddPolicy("MinimumAge", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(18)));

    options.AddPolicy("ResourceAccess", policy =>
        policy.Requirements.Add(new ResourceAccessRequirement()));
});

// Rate Limiting
if (enableRateLimiting)
{
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
    builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
    builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
}

// YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(transformBuilderContext =>
    {
        transformBuilderContext.AddRequestTransform(transformContext =>
        {
            // Add correlation ID
            if (!transformContext.HttpContext.Request.Headers.ContainsKey("X-Correlation-ID"))
            {
                transformContext.ProxyRequest.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());
            }
            return ValueTask.CompletedTask;
        });
    });

// Redis Caching
if (enableCaching)
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = "APIGateway";
    });
    builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
        ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));
}

// Service Discovery
if (enableServiceDiscovery)
{
    builder.Services.AddSingleton<IServiceDiscovery, ConsulServiceDiscovery>();
}

// OpenTelemetry
if (enableOpenTelemetry)
{
    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
            metrics
                .AddPrometheusExporter()
                .AddMeter("APIGateway.Metrics")
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("APIGateway"));
        })
        .WithTracing(tracing =>
        {
            tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddJaegerExporter(options =>
                {
                    options.AgentHost = builder.Configuration["Jaeger:Host"] ?? "localhost";
                    options.AgentPort = int.Parse(builder.Configuration["Jaeger:Port"] ?? "6831");
                });
        });
}

// Custom Services
builder.Services.AddSingleton<ICircuitBreakerService, CircuitBreakerService>();
builder.Services.AddSingleton<IAggregationService, AggregationService>();
builder.Services.AddSingleton<IApiVersioningService, ApiVersioningService>();
builder.Services.AddSingleton<ISecretManager, VaultSecretManager>();
builder.Services.AddSingleton<IGovernanceService, GovernanceService>();
builder.Services.AddSingleton<ICloudNativeService, KubernetesService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

if (enableOpenTelemetry)
{
    app.UseOpenTelemetryPrometheusScrapingEndpoint();
}

app.UseAuthentication();
app.UseAuthorization();

if (enableRateLimiting)
{
    app.UseIpRateLimiting();
}

// Custom Middleware
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<AuthenticationMiddleware>();
app.UseMiddleware<AuthorizationMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<CircuitBreakerMiddleware>();
app.UseMiddleware<CachingMiddleware>();
app.UseMiddleware<LoggingMiddleware>();

// Health Check
app.MapHealthChecks("/health");

// YARP Proxy
app.MapReverseProxy(proxyPipeline =>
{
    if (enableCircuitBreaker)
    {
        proxyPipeline.UseCircuitBreaker();
    }
});

// API Endpoints
app.MapGroup("/api/v{version:apiVersion}")
    .MapApiEndpoints();

app.Run();