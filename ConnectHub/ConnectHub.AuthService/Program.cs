using DotNetEnv;
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

// ========== LOAD .env FILE ==========
// This loads your secrets from .env file (which is ignored by Git)
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// ========== 1. DATABASE CONFIGURATION (from .env) ==========
var connectionString = $"Host={Env.GetString("DB_HOST")};" +
                       $"Port={Env.GetString("DB_PORT")};" +
                       $"Database={Env.GetString("DB_NAME")};" +
                       $"Username={Env.GetString("DB_USER")};" +
                       $"Password={Env.GetString("DB_PASSWORD")}";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// ========== 2. JWT SETTINGS (from .env) ==========
var jwtSecret = Env.GetString("JWT_SECRET");
var jwtExpiration = int.Parse(Env.GetString("JWT_EXPIRATION"));

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

// ========== 4. GOOGLE OAUTH (from .env) ==========
builder.Services.Configure<GoogleAuthSettings>(options =>
{
    options.ClientId = Env.GetString("GOOGLE_CLIENT_ID");
    options.ClientSecret = Env.GetString("GOOGLE_CLIENT_SECRET");
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
});

// ========== 8. CORS ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// ========== BUILD THE APP ==========
var app = builder.Build();

// ========== 9. MIDDLEWARE ==========
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ========== 10. AUTO-MIGRATE DATABASE ==========
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

// Display URLs
Console.WriteLine(" Auth Service is running!");
Console.WriteLine("Swagger UI: https://localhost:5001/swagger");
Console.WriteLine("Swagger UI (HTTP): http://localhost:5000/swagger");

app.Run();