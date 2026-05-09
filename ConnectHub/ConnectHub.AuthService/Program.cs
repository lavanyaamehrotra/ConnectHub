using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Linq;
using ConnectHub.AuthService.Data;
using ConnectHub.AuthService.Config;
using ConnectHub.AuthService.Helpers;
using ConnectHub.AuthService.Interfaces;
using ConnectHub.AuthService.Repositories;
using ConnectHub.AuthService.Services;

// NOTE: No more DotNetEnv / Env.Load() needed.
// - Local dev  → reads from appsettings.Development.json
// - Docker     → reads from appsettings.Docker.json + docker-compose environment variables
// Both work automatically via builder.Configuration

var builder = WebApplication.CreateBuilder(args);

// ========== 1. DATABASE CONFIGURATION ==========
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// ========== 2. JWT SETTINGS ==========
var jwtSecret = builder.Configuration["JWT:Secret"]
    ?? throw new InvalidOperationException("JWT:Secret not configured.");
var jwtExpiration = int.TryParse(builder.Configuration["JWT:ExpirationInMinutes"], out var exp) ? exp : 60;

var jwtSettings = new JwtSettings
{
    Secret = jwtSecret,
    ExpirationInMinutes = jwtExpiration
};
builder.Services.Configure<JwtSettings>(options =>
{
    options.Secret = jwtSecret;
    options.ExpirationInMinutes = jwtExpiration;
});
builder.Services.AddSingleton(jwtSettings);

// ========== 3. JWT AUTHENTICATION ==========
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(jwtSettings.GetSecretBytes()),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

// ========== 4. GOOGLE OAUTH ==========
builder.Services.Configure<GoogleAuthSettings>(options =>
{
    options.ClientId = builder.Configuration["Google:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Google:ClientSecret"] ?? "";
});

// ========== 5. DEPENDENCY INJECTION ==========
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<GoogleAuthHelper>();

// ========== 6. CONTROLLERS ==========
builder.Services.AddControllers();

// ========== 7. SWAGGER ==========
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ConnectHub Auth API",
        Version = "v1",
        Description = "Authentication and User Management API for ConnectHub Chat"
    });

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
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // INCLUDE XML COMMENTS - This makes your controller comments show up in Swagger UI
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
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

// ========== BUILD THE APP ==========
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontends");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ========== 10. AUTO-MIGRATE & CLEANUP DATABASE ==========
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    Console.WriteLine("Applying Database Migrations...");
    try {
        dbContext.Database.Migrate();
        Console.WriteLine("Auth Service migrations applied.");
    } catch (Exception ex) {
        Console.WriteLine($"Migration skip: {ex.Message} (Continuing to Admin check...)");
    }

    // 👑 RELIABLE ADMIN PROMOTION
    var adminEmail = "lavanyamehrotra74@gmail.com";
    Console.WriteLine($"[SECURITY] Checking Admin status for {adminEmail}...");
    var user = dbContext.Users.FirstOrDefault(u => u.Email.ToLower() == adminEmail.ToLower());
    if (user != null)
    {
        if (user.Role != "ADMIN")
        {
            user.Role = "ADMIN";
            dbContext.SaveChanges();
            Console.WriteLine($"[SECURITY] User {adminEmail} has been PROMOTED to ADMIN.");
        }
        else
        {
            Console.WriteLine($"[SECURITY] User {adminEmail} is already an ADMIN.");
        }
    }
    else
    {
        Console.WriteLine($"[SECURITY] User {adminEmail} not found. Will promote on next login.");
    }

    Console.WriteLine("Auth Service is fully synchronized!");


    // Cleanup stale online statuses
    try {
        dbContext.Database.ExecuteSqlRaw("UPDATE \"Users\" SET \"IsOnline\" = false");
        Console.WriteLine("Auth Service: Cleaned up stale online statuses.");
    } catch (Exception ex) {
        Console.WriteLine($"Auth Service: Cleanup failed: {ex.Message}");
    }
}

Console.WriteLine("Auth Service is running!");
Console.WriteLine("Swagger UI: http://localhost:5000/swagger");

app.Run();