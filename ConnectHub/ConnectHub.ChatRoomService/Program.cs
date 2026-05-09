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
    throw new Exception("Database connection string not found");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

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
        policy.WithOrigins(
                "http://localhost:4200", 
                "http://localhost:3000", 
                "http://localhost:5000", 
                "http://localhost:5003",
                "https://connecthub-frontend-f8dq.onrender.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GroupChatHub>("/groupChatHub");

try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // 🏛️ GOD MODE: MANUAL TABLE BUILDER
    try {
        Console.WriteLine("Manually verifying tables...");
        
        // 1. chatrooms
        await dbContext.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""ChatRooms"" (
                ""RoomId"" uuid NOT NULL PRIMARY KEY,
                ""Name"" character varying(100) NOT NULL,
                ""Description"" character varying(500),
                ""RoomType"" character varying(20) NOT NULL,
                ""CreatedBy"" uuid NOT NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""MaxMembers"" integer NOT NULL,
                ""IsActive"" boolean NOT NULL,
                ""AvatarUrl"" character varying(500)
            )");

        // 2. roommembers
        await dbContext.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""RoomMembers"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""RoomId"" uuid NOT NULL,
                ""UserId"" uuid NOT NULL,
                ""Username"" character varying(50) NOT NULL,
                ""Role"" character varying(20) NOT NULL,
                ""JoinedAt"" timestamp with time zone NOT NULL,
                ""IsActive"" boolean NOT NULL,
                ""LastReadMessageId"" uuid,
                ""LastReadAt"" timestamp with time zone
            )");

        // 3. roommessages
        await dbContext.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""RoomMessages"" (
                ""MessageId"" uuid NOT NULL PRIMARY KEY,
                ""RoomId"" uuid NOT NULL,
                ""SenderId"" uuid NOT NULL,
                ""Content"" character varying(2000),
                ""SentAt"" timestamp with time zone NOT NULL,
                ""MessageType"" text,
                ""MediaUrl"" text,
                ""IsRead"" boolean NOT NULL DEFAULT false,
                ""ReadAt"" timestamp with time zone,
                ""IsDeleted"" boolean NOT NULL DEFAULT false
            )");

        // 🌱 AUTO-SEED DEFAULT ROOM
        if (!dbContext.ChatRooms.Any())
        {
            var generalRoom = new ChatRoom
            {
                RoomId = Guid.NewGuid(),
                RoomName = "General Discussion",
                Description = "Welcome to ConnectHub!",
                RoomType = "PUBLIC",
                CreatedBy = Guid.Empty,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            dbContext.ChatRooms.Add(generalRoom);
            await dbContext.SaveChangesAsync();
            Console.WriteLine("Seed: 'General Discussion' room created.");
        }

        Console.WriteLine("Manual table verification and seeding complete.");
    } catch (Exception ex) {
        Console.WriteLine($"Manual builder skip: {ex.Message}");
    }

    Console.WriteLine("ChatRoom Service is 100% ONLINE!");
}
catch (Exception ex)
{
    Console.WriteLine($"DATABASE ERROR: {ex.Message}");
}

Console.WriteLine("ChatRoom Service running!");
Console.WriteLine("Docker -> Swagger: http://localhost:5004/swagger");
Console.WriteLine("Dev    -> Swagger: http://localhost:5005/swagger");

await app.RunAsync();