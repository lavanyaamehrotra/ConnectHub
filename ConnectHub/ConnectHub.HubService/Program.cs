using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using ConnectHub.HubService.Hubs;
using ConnectHub.HubService.Interfaces;
using ConnectHub.HubService.Presence;
using ConnectHub.HubService.Services;

// ============================================================
// ConnectHub.HubService — UC4 Redis Update
//
// CHANGE FROM ORIGINAL:
//   OLD: builder.Services.AddSingleton<IPresenceService, PresenceService>();
//   NEW: Register Redis IConnectionMultiplexer first, then PresenceService
//        is still Singleton but now backed by Redis.
//
// Everything else is unchanged.
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// ========== 1. JWT AUTHENTICATION ==========
var jwtSecret = builder.Configuration["JWT:Secret"]
    ?? "This-Is-My-Super-Secret-Key-For-JWT-At-Least-32-Characters-Long-ChangeThis!";

var keyBytes = System.Text.Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer           = false,
            ValidateAudience         = false,
            ClockSkew                = TimeSpan.Zero
        };

        // Required for SignalR: browser can't set Authorization header on WebSocket
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path        = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();


// ========== 3. SIGNALR ==========
builder.Services.AddSignalR();

// ========== 3. CUSTOM USER ID PROVIDER ==========
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

// ========== 4. REDIS — UC4 CHANGE ==========
// Read Redis connection string from config (falls back to localhost for dev)
var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";

// IConnectionMultiplexer is thread-safe and should be Singleton
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnection));

// PresenceService is now backed by Redis (still registered as Singleton)
builder.Services.AddSingleton<IPresenceService, PresenceService>();

// ========== 5. HTTP CLIENTS (Typed) for downstream microservices ==========
builder.Services.AddHttpClient<IMessageService, MessageServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:MessageService"] ?? "http://localhost:5003");
    client.Timeout     = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<IChatRoomService, ChatRoomServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ChatRoomService"] ?? "http://chatroom-service:5004");
    client.Timeout     = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<INotificationServiceClient, NotificationServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:NotificationService"] ?? "http://notification-service:5007");
    client.Timeout     = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<IAuthServiceClient, AuthServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:AuthService"] ?? "http://auth-service:5000");
    client.Timeout     = TimeSpan.FromSeconds(10);
});

// ========== 6. SERVICE INTERFACES ==========
// (Typed clients are already registered above)


// ========== 7. CONTROLLERS + SWAGGER ==========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "ConnectHub Hub Service",
        Version     = "v1",
        Description = "SignalR ChatHub + Redis Presence Tracking"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter: Bearer {your JWT token}",
        Name        = "Authorization",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.ApiKey,
        Scheme      = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ========== 8. CORS ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontends", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200", 
                "http://localhost:3000",
                "https://connecthub-frontend-f8dq.onrender.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ========== BUILD ==========
var app = builder.Build();

app.UseCors("AllowFrontends");

// UC4: Purge stale Redis presence data once at startup (prevents race conditions in constructor)
using (var scope = app.Services.CreateScope())
{
    var presenceService = scope.ServiceProvider.GetRequiredService<IPresenceService>();
    await presenceService.PurgeStalePresenceAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

Console.WriteLine("==============================================");
Console.WriteLine("  ConnectHub HubService (UC4 — Redis Presence)");
Console.WriteLine("==============================================");
Console.WriteLine("  SignalR:  ws://localhost:5006/hubs/chat");
Console.WriteLine("  Presence: http://localhost:5006/api/presence");
Console.WriteLine("  Swagger:  http://localhost:5006/swagger");
Console.WriteLine("  Redis:    " + redisConnection);
Console.WriteLine("==============================================");

await app.RunAsync();