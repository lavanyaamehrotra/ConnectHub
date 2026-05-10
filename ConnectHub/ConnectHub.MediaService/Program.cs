using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ConnectHub.MediaService.Data;
using ConnectHub.MediaService.Interfaces;
using ConnectHub.MediaService.Repositories;
using ConnectHub.MediaService.Services;

// ============================================================
// ConnectHub.MediaService — UC6
// File/Media Service with Azure Blob Storage
// Port: 5008
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// ========== 1. DATABASE ==========
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connStr))
{
    Console.WriteLine("ERROR: MediaService connection string missing! Using fallback.");
    connStr = "Host=localhost;Database=Dummy;Username=dummy;Password=dummy";
}
else 
{
    Console.WriteLine("MediaService: Connection string found (length: " + connStr.Length + ")");
}


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connStr, x => x.MigrationsHistoryTable("__EFMigrationsHistory_Media")));

// ========== 2. AZURE BLOB STORAGE ==========
var blobConnectionString = builder.Configuration["Azure:BlobConnectionString"]
    ?? "UseDevelopmentStorage=true"; // Uses Azurite emulator if not configured

builder.Services.AddSingleton(new BlobServiceClient(blobConnectionString));

// ========== 3. JWT AUTHENTICATION ==========
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

// ========== 4. REPOSITORY + SERVICE ==========
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IMediaService, MediaService>();

// ========== 5. BACKGROUND SERVICE — daily cleanup ==========
builder.Services.AddHostedService<ExpiredFileCleanupService>();

// ========== 6. MULTIPART FORM (large file uploads) ==========
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100 MB max
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100 MB
});

// ========== 7. CONTROLLERS + SWAGGER ==========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "ConnectHub Media Service",
        Version     = "v1",
        Description = "UC6 — File upload, Azure Blob Storage, SAS URL generation, cleanup"
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
                "http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader();
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
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Console.WriteLine("Applying database migrations for MediaService...");
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("MediaService: Database migration completed.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"CRITICAL ERROR: MediaService migration failed: {ex.Message}");
    }
});



app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("==============================================");
Console.WriteLine("  ConnectHub MediaService (UC6)");
Console.WriteLine("==============================================");
Console.WriteLine("  REST:    http://localhost:5008/api/media");
Console.WriteLine("  Swagger: http://localhost:5008/swagger");
Console.WriteLine("  Azure:   " + (builder.Configuration["Azure:BlobConnectionString"] != null
    ? "Azure Blob Storage configured"
    : "Using Azurite (local emulator)"));
Console.WriteLine("==============================================");

await app.RunAsync();
