namespace ConnectHub.GatewayService;

/// <summary>
/// Keeps all Render free-tier microservices awake by pinging them every 14 minutes.
/// Render sleeps idle services after 15 minutes — this prevents cold-start 502 errors.
/// </summary>
public class WarmupService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WarmupService> _logger;

    // Ping these lightweight endpoints to wake up each service
    private static readonly (string Name, string Url)[] ServiceEndpoints =
    [
        ("AuthService",         "https://auth-service-kt3x.onrender.com/swagger/index.html"),
        ("MessageService",      "https://message-service-p29m.onrender.com/swagger/index.html"),
        ("ChatRoomService",     "https://chatroom-service-av9h.onrender.com/swagger/index.html"),
        ("HubService",          "https://hub-service-4xti.onrender.com/swagger/index.html"),
        ("NotificationService", "https://notification-service-gduz.onrender.com/swagger/index.html"),
        ("MediaService",        "https://media-service-os9l.onrender.com/swagger/index.html"),
    ];

    public WarmupService(IHttpClientFactory httpClientFactory, ILogger<WarmupService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give the gateway itself 5s to finish starting up first
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        // Warm up immediately on startup
        _logger.LogInformation("[Warmup] Gateway started — waking up all backend services...");
        await PingAllServices();

        // Then keep-alive ping every 14 minutes
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(14));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            _logger.LogInformation("[Warmup] Keep-alive ping to all services...");
            await PingAllServices();
        }
    }

    private async Task PingAllServices()
    {
        var client = _httpClientFactory.CreateClient("WarmupClient");

        var tasks = ServiceEndpoints.Select(async svc =>
        {
            try
            {
                var response = await client.GetAsync(svc.Url);
                _logger.LogInformation("[Warmup] ✅ {Name} is awake (HTTP {Status})", svc.Name, (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[Warmup] ⚠️ {Name} ping failed: {Error}", svc.Name, ex.Message);
            }
        });

        await Task.WhenAll(tasks);
        _logger.LogInformation("[Warmup] All services pinged.");
    }
}
