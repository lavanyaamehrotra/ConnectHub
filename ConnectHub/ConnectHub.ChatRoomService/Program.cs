using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ConnectHub.ChatRoomService.Data;
using ConnectHub.ChatRoomService.Interfaces;
using ConnectHub.ChatRoomService.Services;
using ConnectHub.ChatRoomService.Hubs;
using ConnectHub.ChatRoomService.Repositories;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("ERROR: Database connection string 'DefaultConnection' not found! Using dummy to prevent DI crash.");
    connectionString = "Host=localhost;Database=Dummy;Username=dummy;Password=dummy";
}
else 
{
    Console.WriteLine("ChatRoomService: Connection string found (length: " + connectionString.Length + ")");
}



builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory_ChatRoom")));

var jwtSecret = builder.Configuration["JWT:Secret"]
    ?? "This-Is-My-Super-Secret-Key-For-JWT-At-Least-32-Characters-Long-ChangeThis!";

var key = System.Text.Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/groupChatHub"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
builder.Services.AddScoped<IChatRoomService, ChatRoomService>();
builder.Services.AddHttpClient();
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ConnectHub ChatRoom API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter 'Bearer' followed by your JWT token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:3000", "http://localhost:5000", "http://localhost:5003")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseSwagger();

app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GroupChatHub>("/groupChatHub");

// Ensure Database is ready BEFORE app runs
try 
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    Console.WriteLine("ChatRoomService: STARTING ULTIMATE RESET...");
    
    // 1. Terminate other connections
    await dbContext.Database.ExecuteSqlRawAsync(@"
        SELECT pg_terminate_backend(pid) FROM pg_stat_activity 
        WHERE datname = current_database() AND pid <> pg_backend_pid();
    ");
    Console.WriteLine("ChatRoomService: Other connections terminated.");

    // 2. Force drop tables one by one with logging
    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS \"RoomMessages\" DROP COLUMN IF EXISTS \"IsRead\" CASCADE;"); } catch { }
    try { await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE IF EXISTS \"RoomMembers\" DROP COLUMN IF EXISTS \"Username\" CASCADE;"); } catch { }
    
    await dbContext.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"RoomMembers\" CASCADE;");
    Console.WriteLine("ChatRoomService: Dropped RoomMembers.");

    await dbContext.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"RoomMessages\" CASCADE;");
    Console.WriteLine("ChatRoomService: Dropped RoomMessages.");

    await dbContext.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"ChatRooms\" CASCADE;");
    Console.WriteLine("ChatRoomService: Dropped ChatRooms.");

    await dbContext.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"__EFMigrationsHistory_ChatRoom\" CASCADE;");
    Console.WriteLine("ChatRoomService: Dropped History Table.");

    Console.WriteLine("ChatRoomService: Starting Migrations...");
    await dbContext.Database.MigrateAsync();
    Console.WriteLine("ChatRoomService: Database is READY.");
}
catch (Exception ex)
{
    Console.WriteLine($"CRITICAL ERROR during ChatRoomService startup: {ex.Message}");
}


Console.WriteLine("ChatRoom Service running!");
Console.WriteLine("Docker -> Swagger: http://localhost:5004/swagger");
Console.WriteLine("Dev    -> Swagger: http://localhost:5005/swagger");

await app.RunAsync();