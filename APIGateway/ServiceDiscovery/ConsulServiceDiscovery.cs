using Consul;

namespace APIGateway.ServiceDiscovery;

public interface IServiceDiscovery
{
    Task<List<ServiceInstance>> DiscoverServicesAsync(string serviceName);
    Task RegisterServiceAsync(string serviceId, string serviceName, string address, int port);
    Task DeregisterServiceAsync(string serviceId);
}

public class ConsulServiceDiscovery : IServiceDiscovery
{
    private readonly IConsulClient _consulClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConsulServiceDiscovery> _logger;

    public ConsulServiceDiscovery(IConfiguration configuration, ILogger<ConsulServiceDiscovery> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var consulAddress = configuration["Consul:Address"] ?? "http://localhost:8500";
        _consulClient = new ConsulClient(config =>
        {
            config.Address = new Uri(consulAddress);
        });
    }

    public async Task<List<ServiceInstance>> DiscoverServicesAsync(string serviceName)
    {
        try
        {
            var services = await _consulClient.Health.Service(serviceName, "", true);
            return services.Response.Select(s => new ServiceInstance
            {
                Id = s.Service.ID,
                Name = s.Service.Service,
                Address = s.Service.Address,
                Port = s.Service.Port,
                HealthStatus = s.Checks.All(c => c.Status == HealthStatus.Passing) ? "Healthy" : "Unhealthy"
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering services from Consul");
            return new List<ServiceInstance>();
        }
    }

    public async Task RegisterServiceAsync(string serviceId, string serviceName, string address, int port)
    {
        try
        {
            var registration = new AgentServiceRegistration
            {
                ID = serviceId,
                Name = serviceName,
                Address = address,
                Port = port,
                Check = new AgentServiceCheck
                {
                    HTTP = $"http://{address}:{port}/health",
                    Interval = TimeSpan.FromSeconds(10),
                    Timeout = TimeSpan.FromSeconds(5),
                    DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
                }
            };

            await _consulClient.Agent.ServiceRegister(registration);
            _logger.LogInformation($"Service {serviceName} registered with Consul");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering service with Consul");
        }
    }

    public async Task DeregisterServiceAsync(string serviceId)
    {
        try
        {
            await _consulClient.Agent.ServiceDeregister(serviceId);
            _logger.LogInformation($"Service {serviceId} deregistered from Consul");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deregistering service from Consul");
        }
    }
}

public class ServiceInstance
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
}

public class KubernetesServiceDiscovery : IServiceDiscovery
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KubernetesServiceDiscovery> _logger;

    public KubernetesServiceDiscovery(IConfiguration configuration, ILogger<KubernetesServiceDiscovery> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<List<ServiceInstance>> DiscoverServicesAsync(string serviceName)
    {
        // Kubernetes service discovery implementation
        // In real implementation, use KubernetesClient to query services
        return Task.FromResult(new List<ServiceInstance>());
    }

    public Task RegisterServiceAsync(string serviceId, string serviceName, string address, int port)
    {
        // Kubernetes handles registration automatically
        return Task.CompletedTask;
    }

    public Task DeregisterServiceAsync(string serviceId)
    {
        // Kubernetes handles deregistration automatically
        return Task.CompletedTask;
    }
}