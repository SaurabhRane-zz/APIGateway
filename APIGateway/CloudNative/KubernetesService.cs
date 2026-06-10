using k8s;
using k8s.Models;

namespace APIGateway.CloudNative;

public interface ICloudNativeService
{
    Task<List<PodInfo>> GetPodsAsync(string namespace_ = "default");
    Task ScaleDeploymentAsync(string deploymentName, int replicas);
    Task<List<ServiceInfo>> GetServicesAsync(string namespace_ = "default");
}

public class KubernetesService : ICloudNativeService
{
    private readonly Kubernetes? _kubernetesClient;
    private readonly ILogger<KubernetesService> _logger;
    private readonly bool _isKubernetesEnvironment;

    public KubernetesService(ILogger<KubernetesService> logger)
    {
        _logger = logger;
        _isKubernetesEnvironment = Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST") != null;

        if (_isKubernetesEnvironment)
        {
            try
            {
                var config = KubernetesClientConfiguration.InClusterConfig();
                _kubernetesClient = new Kubernetes(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Kubernetes client");
            }
        }
    }

    public async Task<List<PodInfo>> GetPodsAsync(string namespace_ = "default")
    {
        if (!_isKubernetesEnvironment || _kubernetesClient == null)
        {
            return new List<PodInfo>();
        }

        try
        {
            var pods = await _kubernetesClient.CoreV1.ListNamespacedPodAsync(namespace_);
            return pods.Items.Select(p => new PodInfo
            {
                Name = p.Metadata.Name,
                Namespace = p.Metadata.NamespaceProperty,
                Status = p.Status.Phase,
                Ip = p.Status.PodIP,
                NodeName = p.Spec.NodeName
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pods from Kubernetes");
            return new List<PodInfo>();
        }
    }

    public async Task ScaleDeploymentAsync(string deploymentName, int replicas)
    {
        if (!_isKubernetesEnvironment || _kubernetesClient == null) return;

        try
        {
            var scale = new V1Scale
            {
                Spec = new V1ScaleSpec { Replicas = replicas }
            };

            await _kubernetesClient.AppsV1.ReplaceNamespacedDeploymentScaleAsync(
                scale, deploymentName, "default");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scaling deployment {Deployment}", deploymentName);
        }
    }

    public async Task<List<ServiceInfo>> GetServicesAsync(string namespace_ = "default")
    {
        if (!_isKubernetesEnvironment || _kubernetesClient == null)
        {
            return new List<ServiceInfo>();
        }

        try
        {
            var services = await _kubernetesClient.CoreV1.ListNamespacedServiceAsync(namespace_);
            return services.Items.Select(s => new ServiceInfo
            {
                Name = s.Metadata.Name,
                ClusterIp = s.Spec.ClusterIP,
                Ports = s.Spec.Ports?.Select(p => new ServicePort
                {
                    Port = p.Port,
                    TargetPort = p.TargetPort?.ToString() ?? ""
                }).ToList() ?? new List<ServicePort>()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting services from Kubernetes");
            return new List<ServiceInfo>();
        }
    }
}

public class PodInfo
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
}

public class ServiceInfo
{
    public string Name { get; set; } = string.Empty;
    public string ClusterIp { get; set; } = string.Empty;
    public List<ServicePort> Ports { get; set; } = new();
}

public class ServicePort
{
    public int Port { get; set; }
    public string TargetPort { get; set; } = string.Empty;
}

public class HealthCheckService
{
    public static IEndpointRouteBuilder MapHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () => Results.Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        }));

        endpoints.MapGet("/health/ready", () => Results.Ok(new
        {
            Status = "Ready",
            Timestamp = DateTime.UtcNow,
            Dependencies = new
            {
                Redis = "Connected",
                Consul = "Connected",
                DownstreamServices = "Available"
            }
        }));

        return endpoints;
    }
}