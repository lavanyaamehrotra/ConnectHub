using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ConnectHub.HubService.Hubs;
using ConnectHub.HubService.Interfaces;
using ConnectHub.HubService.Presence;
using ConnectHub.HubService.Services;

// ============================================================
// ConnectHub.HubService — Standalone Real-Time Microservice
//
// FROM CLASS DIAGRAM (Section 4.4):
//   • ChatHub registered at /hubs/chat
//   • IPresenceService registered as AddSingleton so the same
//     ConcurrentDictionary<int, HashSet<string>> instance is
//     shared across ALL Hub connections and API controllers.
//   • IMessageService  registered as Scoped (HTTP client wrapper)
//   • IChatRoomService registered as Scoped (HTTP client wrapper)
//
// PORT: 5006
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

        // CRITICAL FOR SIGNALR:
        // Browsers cannot set Authorization headers on WebSocket connections.
        // SignalR JS client sends JWT as query string: ?access_token=eyJ...
        // This event reads it and places it where JwtBearer expects it.
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

// ========== 2. SIGNALR ==========
builder.Services.AddSignalR();

// ========== 3. CUSTOM USER ID PROVIDER (Singleton) ==========
// Maps JWT NameIdentifier claim → SignalR user identifier.
// Makes Clients.User(userId) deliver to ALL connections for that user.
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

// ========== 4. PRESENCE SERVICE (Singleton) ==========
// FROM CLASS DIAGRAM:
// "IPresenceService is registered as AddSingleton so the same
//  ConcurrentDictionary<int, HashSet<string>> instance is shared
//  across all Hub connections and API controllers."
builder.Services.AddSingleton<IPresenceService, PresenceService>();

// ========== 5. HTTP CLIENTS for downstream microservices ==========
var messageServiceUrl  = builder.Configuration["Services:MessageService"]  ?? "http://localhost:5003";
var chatRoomServiceUrl = builder.Configuration["Services:ChatRoomService"] ?? "http://localhost:5005";

builder.Services.AddHttpClient("MessageService", client =>
{
    client.BaseAddress = new Uri(messageServiceUrl);
    client.Timeout     = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient("ChatRoomService", client =>
{
    client.BaseAddress = new Uri(chatRoomServiceUrl);
    client.Timeout     = TimeSpan.FromSeconds(10);
});

// ========== 6. SERVICE INTERFACES (Scoped) ==========
// FROM CLASS DIAGRAM (ChatHub fields):
//   _messageService  → IMessageService
//   _roomService     → IChatRoomService
// Registered Scoped: a new instance per Hub invocation, but they
// themselves are stateless HTTP wrappers backed by IHttpClientFactory.
builder.Services.AddScoped<IMessageService,  MessageServiceClient>();
builder.Services.AddScoped<IChatRoomService, ChatRoomServiceClient>();

// ========== 7. CONTROLLERS ==========
builder.Services.AddControllers();

// ========== 8. SWAGGER ==========
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "ConnectHub Hub Service",
        Version     = "v1",
        Description = "SignalR ChatHub + Presence Tracking. Hub: ws://localhost:5006/hubs/chat"
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
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ========== 9. CORS ==========
// AllowCredentials() is REQUIRED for SignalR WebSocket upgrade.
// Must use WithOrigins() (not AllowAnyOrigin) with AllowCredentials.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontends", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",   // Angular
                "http://localhost:3000",   // React
                "http://localhost:5173",   // Vite / Vue
                "http://localhost:5000",   // Auth service
                "http://localhost:5003",   // Message service
                "http://localhost:5005"    // ChatRoom service
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // REQUIRED for SignalR WebSocket!
    });
});

// ========== BUILD ==========
var app = builder.Build();

// ========== MIDDLEWARE PIPELINE ==========
// ORDER MATTERS: UseCors → UseAuthentication → UseAuthorization → MapHub

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontends");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// MAP THE SIGNALR HUB
// Client connects to: ws://localhost:5006/hubs/chat?access_token=eyJ...
app.MapHub<ChatHub>("/hubs/chat");

// ========== STARTUP LOG ==========
Console.WriteLine("==============================================");
Console.WriteLine("  ConnectHub HubService (UC4 — Real-Time)");
Console.WriteLine("==============================================");
Console.WriteLine("  SignalR:  ws://localhost:5006/hubs/chat");
Console.WriteLine("  Presence: http://localhost:5006/api/presence");
Console.WriteLine("  Swagger:  http://localhost:5006/swagger");
Console.WriteLine("==============================================");

await app.RunAsync();