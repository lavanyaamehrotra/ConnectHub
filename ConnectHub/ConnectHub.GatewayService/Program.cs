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
// Routes and clusters are loaded from appsettings.json (ReverseProxy section).
// This keeps routing config out of code — easy to update without recompiling.
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ========== 3. CORS ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontends", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200", 
                "http://localhost:8080",
                "https://connecthub-frontend-f8dq.onrender.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ========== BUILD ==========
var app = builder.Build();

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

// Map YARP — this handles all proxied routes defined in appsettings.json
app.MapReverseProxy();

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
