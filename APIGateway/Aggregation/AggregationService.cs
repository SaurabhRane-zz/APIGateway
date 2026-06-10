namespace APIGateway.Aggregation;

public interface IAggregationService
{
    Task<T> AggregateAsync<T>(params Func<Task<T>>[] tasks);
    Task<List<T>> FanOutAsync<T>(IEnumerable<Func<Task<T>>> tasks);
}

public class AggregationService : IAggregationService
{
    private readonly ILogger<AggregationService> _logger;

    public AggregationService(ILogger<AggregationService> logger)
    {
        _logger = logger;
    }

    public async Task<T> AggregateAsync<T>(params Func<Task<T>>[] tasks)
    {
        var results = await Task.WhenAll(tasks.Select(async task =>
        {
            try
            {
                return await task();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in aggregation task");
                return default;
            }
        }));

        // Combine results based on type
        return CombineResults<T>(results.Where(r => r != null));
    }

    public async Task<List<T>> FanOutAsync<T>(IEnumerable<Func<Task<T>>> tasks)
    {
        var results = new List<T>();
        var errors = new List<Exception>();

        await Task.WhenAll(tasks.Select(async task =>
        {
            try
            {
                var result = await task();
                if (result != null)
                {
                    lock (results)
                    {
                        results.Add(result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in fan-out task");
                lock (errors)
                {
                    errors.Add(ex);
                }
            }
        }));

        return results;
    }

    private T CombineResults<T>(IEnumerable<T> results)
    {
        // Implementation depends on T type
        return results.FirstOrDefault()!;
    }
}

public class BackendForFrontendService
{
    private readonly HttpClient _httpClient;
    private readonly IAggregationService _aggregationService;

    public BackendForFrontendService(HttpClient httpClient, IAggregationService aggregationService)
    {
        _httpClient = httpClient;
        _aggregationService = aggregationService;
    }

    public async Task<UserDashboard> GetUserDashboardAsync(string userId)
    {
        var profileTask = FetchUserProfile(userId);
        var ordersTask = FetchUserOrders(userId);
        var recommendationsTask = FetchRecommendations(userId);
        var notificationsTask = FetchNotifications(userId);

        return await _aggregationService.AggregateAsync(
            () => profileTask,
            () => ordersTask,
            () => recommendationsTask,
            () => notificationsTask
        );
    }

    private async Task<UserProfile> FetchUserProfile(string userId)
    {
        var response = await _httpClient.GetAsync($"/api/users/{userId}");
        return await response.Content.ReadFromJsonAsync<UserProfile>() ?? new UserProfile();
    }

    private async Task<List<Order>> FetchUserOrders(string userId)
    {
        var response = await _httpClient.GetAsync($"/api/orders?userId={userId}");
        return await response.Content.ReadFromJsonAsync<List<Order>>() ?? new List<Order>();
    }

    private async Task<List<Recommendation>> FetchRecommendations(string userId)
    {
        var response = await _httpClient.GetAsync($"/api/recommendations?userId={userId}");
        return await response.Content.ReadFromJsonAsync<List<Recommendation>>() ?? new List<Recommendation>();
    }

    private async Task<List<Notification>> FetchNotifications(string userId)
    {
        var response = await _httpClient.GetAsync($"/api/notifications?userId={userId}");
        return await response.Content.ReadFromJsonAsync<List<Notification>>() ?? new List<Notification>();
    }
}

public class UserDashboard
{
    public UserProfile Profile { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
    public List<Recommendation> Recommendations { get; set; } = new();
    public List<Notification> Notifications { get; set; } = new();
}

public class UserProfile { public string UserId { get; set; } = string.Empty; }
public class Order { public string OrderId { get; set; } = string.Empty; }
public class Recommendation { public string ItemId { get; set; } = string.Empty; }
public class Notification { public string NotificationId { get; set; } = string.Empty; }