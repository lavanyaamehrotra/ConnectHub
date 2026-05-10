using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("ERROR: Connection string 'DefaultConnection' not found. Using a dummy string to prevent DI crash.");
    connectionString = "Host=localhost;Database=Dummy;Username=dummy;Password=dummy"; 
}
else 
{
    Console.WriteLine("AuthService: Connection string found (length: " + connectionString.Length + ")");
}



builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory_Auth")));

// ========== 2. JWT SETTINGS ==========
var jwtSecret = builder.Configuration["JWT:Secret"] ?? "TemporaryFallbackSecretForStartup";
if (builder.Configuration["JWT:Secret"] == null)
{
    Console.WriteLine("WARNING: JWT:Secret not configured. Using temporary fallback.");
}

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

// ========== BUILD THE APP ==========
var app = builder.Build();

// ========== 9. MIDDLEWARE ==========
// Swagger always enabled (works in Docker environment too)
app.UseDeveloperExceptionPage(); 
app.UseSwagger();

app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ========== 10. AUTO-MIGRATE & CLEANUP DATABASE ==========
try 
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Console.WriteLine("Applying database migrations for AuthService...");
        dbContext.Database.Migrate();

        // Cleanup stale online statuses
        try {
            dbContext.Database.ExecuteSqlRaw("UPDATE \"Users\" SET \"IsOnline\" = false");
            Console.WriteLine("Auth Service: Cleaned up stale online statuses.");
        } catch (Exception ex) {
            Console.WriteLine($"Auth Service: Cleanup failed: {ex.Message}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"WARNING: AuthService migration failed: {ex.Message}");
}


Console.WriteLine("Auth Service is running!");
Console.WriteLine("Swagger UI: http://localhost:5000/swagger");

app.Run();