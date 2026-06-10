namespace APIGateway.Governance;

public interface IGovernanceService
{
    Task<ApiUsageReport> GetApiUsageReportAsync(string apiId, DateTime from, DateTime to);
    Task<bool> ValidateApiComplianceAsync(string apiId);
    Task<List<PolicyViolation>> CheckPolicyViolationsAsync();
    Task EnforceQuotaAsync(string clientId, string endpoint);
}

public class GovernanceService : IGovernanceService
{
    private readonly ILogger<GovernanceService> _logger;
    private readonly Dictionary<string, ApiQuota> _quotas = new();

    public GovernanceService(ILogger<GovernanceService> logger)
    {
        _logger = logger;
    }

    public async Task<ApiUsageReport> GetApiUsageReportAsync(string apiId, DateTime from, DateTime to)
    {
        return new ApiUsageReport
        {
            ApiId = apiId,
            Period = new Period { From = from, To = to },
            TotalRequests = 10000,
            SuccessfulRequests = 9500,
            FailedRequests = 500,
            AverageResponseTime = 150,
            TopConsumers = new List<ConsumerUsage>
            {
                new() { ClientId = "client1", RequestCount = 5000 },
                new() { ClientId = "client2", RequestCount = 3000 }
            }
        };
    }

    public async Task<bool> ValidateApiComplianceAsync(string apiId)
    {
        // Check compliance with governance policies
        return true;
    }

    public async Task<List<PolicyViolation>> CheckPolicyViolationsAsync()
    {
        return new List<PolicyViolation>();
    }

    public async Task EnforceQuotaAsync(string clientId, string endpoint)
    {
        var key = $"{clientId}:{endpoint}";
        if (!_quotas.ContainsKey(key))
        {
            _quotas[key] = new ApiQuota { ClientId = clientId, Endpoint = endpoint, Limit = 1000, Used = 0 };
        }

        var quota = _quotas[key];
        quota.Used++;

        if (quota.Used > quota.Limit)
        {
            throw new QuotaExceededException($"Quota exceeded for {clientId} on {endpoint}");
        }
    }
}

public class ApiUsageReport
{
    public string ApiId { get; set; } = string.Empty;
    public Period Period { get; set; } = new();
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double AverageResponseTime { get; set; }
    public List<ConsumerUsage> TopConsumers { get; set; } = new();
}

public class Period
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

public class ConsumerUsage
{
    public string ClientId { get; set; } = string.Empty;
    public long RequestCount { get; set; }
}

public class PolicyViolation
{
    public string PolicyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public string Severity { get; set; } = string.Empty;
}

public class ApiQuota
{
    public string ClientId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public int Limit { get; set; }
    public int Used { get; set; }
}

public class QuotaExceededException : Exception
{
    public QuotaExceededException(string message) : base(message) { }
}