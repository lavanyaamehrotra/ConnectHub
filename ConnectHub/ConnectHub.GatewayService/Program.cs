using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// ============================================================
// ConnectHub.GatewayService — YARP API Gateway
//
// Single entry point for all ConnectHub microservices.
// Routes:
//   /api/auth/**          → AuthService        :5000
//   /api/users/**         → AuthService        :5000
//   /api/messages/**      → MessageService     :5003
//   /api/chatrooms/**     → ChatRoomService    :5004 (Docker) / 5005 (Dev)
//   /hubs/chat            → HubService         :5006  (SignalR WebSocket)
//   /api/presence/**      → HubService         :5006
//   /api/notify/**        → HubService         :5006
//   /api/notifications/** → NotificationService:5007
//   /api/media/**         → MediaService       :5008
//
// Port: 5009 (local dev) / 80 (Docker)
// Swagger per-service: each service still has its own /swagger
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// ========== 1. JWT AUTHENTICATION (validate at Gateway) ==========
// The Gateway validates the JWT so downstream services are shielded.
// All services share the same secret — tokens issued by AuthService
// are trusted by YARP before forwarding.

var jwtSecret = builder.Configuration["JWT:Secret"]
    ?? "This-Is-My-Super-Secret-Key-For-JWT-At-Least-32-Characters-Long-ChangeThis!";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           System.Text.Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer           = false,
            ValidateAudience         = false,
            ClockSkew                = TimeSpan.Zero
        };

        // Required for SignalR WebSocket — browser sends token as query param
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path        = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ========== 2. YARP REVERSE PROXY ==========
// We load from config — YARP will automatically pick up your 
// ReverseProxy__Clusters__... variables from Render!
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ========== 2b. WARMUP SERVICE ==========
// Keeps all Render free-tier microservices awake (ping every 14 min).
// Prevents cold-start 502/429 errors for the first user of the day.
builder.Services.AddHttpClient("WarmupClient", client =>
{
    client.Timeout = TimeSpan.FromMinutes(2);
});
builder.Services.AddHostedService<WarmupService>();

// ========== 3. CORS ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontends", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200", 
                "https://connecthub-frontend-f8dq.onrender.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ========== BUILD ==========
var app = builder.Build();

app.UseRouting();

// ========== GOOGLE LOGIN & SECURITY HEADERS ==========
app.Use(async (context, next) =>
{
    // Required for Google OAuth Popups to work correctly
    context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin-allow-popups");
    context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "unsafe-none");
    
    // Security Best Practices
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    
    await next();
});

app.UseCors("AllowFrontends");
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint — useful for Docker healthcheck / load balancer probes
app.MapGet("/health", () => Results.Ok(new
{
    status  = "healthy",
    service = "ConnectHub Gateway",
    time    = DateTime.UtcNow
}));

// Map YARP with error logging
app.UseWebSockets();

app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use(async (context, next) =>
    {
        await next();
        var proxyFeature = context.GetReverseProxyFeature();
        if (proxyFeature?.ProxiedDestination != null && context.Response.StatusCode >= 400)
        {
            Console.WriteLine($"[YARP] Error: {context.Response.StatusCode} when proxying {context.Request.Path} to {proxyFeature.ProxiedDestination.Model.Config.Address}");
        }
    });
});


Console.WriteLine("==============================================");
Console.WriteLine("  ConnectHub Gateway (YARP)");
Console.WriteLine("==============================================");
Console.WriteLine("  Gateway:  http://localhost:5009");
Console.WriteLine("  Health:   http://localhost:5009/health");
Console.WriteLine("----------------------------------------------");
Console.WriteLine("  /api/auth/**          → AuthService     :5000");
Console.WriteLine("  /api/users/**         → AuthService     :5000");
Console.WriteLine("  /api/messages/**      → MessageService  :5003");
Console.WriteLine("  /api/chatrooms/**     → ChatRoomService :5005");
Console.WriteLine("  /hubs/chat            → HubService      :5006");
Console.WriteLine("  /api/presence/**      → HubService      :5006");
Console.WriteLine("  /api/notify/**        → HubService      :5006");
Console.WriteLine("  /api/notifications/** → NotificationSvc :5007");
Console.WriteLine("  /api/media/**         → MediaService    :5008");
Console.WriteLine("==============================================");

await app.RunAsync();

// ============================================================
// WarmupService — keeps all Render free-tier services awake.
// Pings every 14 min so services never sleep (Render sleeps at 15 min).
// ============================================================
public class WarmupService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WarmupService> _logger;

    private static readonly (string Name, string Url)[] ServiceEndpoints = new[]
    {
        ("AuthService",         "https://auth-service-kt3x.onrender.com/swagger/index.html"),
        ("MessageService",      "https://message-service-p29m.onrender.com/swagger/index.html"),
        ("ChatRoomService",     "https://chatroom-service-av9h.onrender.com/swagger/index.html"),
        ("HubService",          "https://hub-service-4xti.onrender.com/swagger/index.html"),
        ("NotificationService", "https://notification-service-gduz.onrender.com/swagger/index.html"),
        ("MediaService",        "https://media-service-os9l.onrender.com/swagger/index.html"),
    };

    public WarmupService(IHttpClientFactory httpClientFactory, ILogger<WarmupService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        _logger.LogInformation("[Warmup] Gateway started - waking up all backend services...");
        await PingAllServices();

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
                _logger.LogInformation("[Warmup] {Name} awake (HTTP {Status})", svc.Name, (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[Warmup] {Name} ping failed: {Error}", svc.Name, ex.Message);
            }
        });
        await Task.WhenAll(tasks);
    }
}

