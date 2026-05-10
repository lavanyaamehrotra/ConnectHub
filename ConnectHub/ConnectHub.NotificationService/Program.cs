using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ConnectHub.NotificationService.Data;
using ConnectHub.NotificationService.Interfaces;
using ConnectHub.NotificationService.Messaging;
using ConnectHub.NotificationService.Repositories;
using ConnectHub.NotificationService.Services;

// ============================================================
// ConnectHub.NotificationService — UC5  (FIXED + RabbitMQ)
//
// Changes vs original Program.cs:
//   1. Registered INotificationPublisher → RabbitMqNotificationPublisher
//   2. Registered NotificationConsumer as a hosted background service
//   3. Removed dependency on IHttpClientFactory inside NotificationService
//      (badge push + email now handled by the consumer)
//   4. Added RabbitMQ connection settings from config
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// ========== 1. DATABASE ==========
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connStr))
{
    Console.WriteLine("ERROR: NotificationService connection string missing! Using fallback.");
    connStr = "Host=localhost;Database=Dummy;Username=dummy;Password=dummy";
}
else 
{
    Console.WriteLine("NotificationService: Connection string found (length: " + connStr.Length + ")");
}


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connStr, x => x.MigrationsHistoryTable("__EFMigrationsHistory_Notification")));

// ========== 2. JWT AUTHENTICATION ==========
var jwtSecret = builder.Configuration["JWT:Secret"]
    ?? "This-Is-My-Super-Secret-Key-For-JWT-At-Least-32-Characters-Long-ChangeThis!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
    });

builder.Services.AddAuthorization();

// ========== 3. HTTP CLIENTS (used by NotificationConsumer for badge push + email lookup) ==========
var hubServiceUrl  = builder.Configuration["Services:HubService"]  ?? "http://localhost:5006";
var authServiceUrl = builder.Configuration["Services:AuthService"] ?? "http://localhost:5000";

builder.Services.AddHttpClient("HubService", client =>
{
    client.BaseAddress = new Uri(hubServiceUrl);
    client.Timeout     = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient("AuthService", client =>
{
    client.BaseAddress = new Uri(authServiceUrl);
    client.Timeout     = TimeSpan.FromSeconds(10);
});

// ========== 4. RABBITMQ — publisher + consumer ==========
// Publisher: Singleton — one persistent connection shared across all requests
builder.Services.AddSingleton<INotificationPublisher, RabbitMqNotificationPublisher>();

// Consumer: BackgroundService — runs the queue listener for the lifetime of the app
builder.Services.AddHostedService<NotificationConsumer>();

// ========== 5. REPOSITORY + SERVICE ==========
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService,    NotificationService>();

// ========== 6. CONTROLLERS + SWAGGER ==========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "ConnectHub Notification Service",
        Version     = "v1",
        Description = "UC5 — In-app notifications, RabbitMQ async dispatch, MailKit email, real-time badge push"
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

// ========== BUILD ==========
var app = builder.Build();

// Auto-run EF migrations on startup
// Run migrations in background to prevent startup hang
_ = Task.Run(async () => 
{
    try 
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Console.WriteLine("Applying database migrations for NotificationService in background...");
            await db.Database.MigrateAsync();
            Console.WriteLine("NotificationService: Database migration completed successfully!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"CRITICAL ERROR: NotificationService migration failed: {ex.Message}");
    }
});



app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("==============================================");
Console.WriteLine("  ConnectHub NotificationService (UC5)");
Console.WriteLine("==============================================");
Console.WriteLine("  REST:     http://localhost:5007/api/notifications");
Console.WriteLine("  Swagger:  http://localhost:5007/swagger");
Console.WriteLine("  RabbitMQ: " + (builder.Configuration["RabbitMQ:Host"] ?? "localhost") + ":5672");
Console.WriteLine("==============================================");

await app.RunAsync();