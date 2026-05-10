using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ConnectHub.MessageService.Data;
using ConnectHub.MessageService.Interfaces;
using ConnectHub.MessageService.Hubs;
using ConnectHub.MessageService.Repositories;
using MessageServiceImpl = ConnectHub.MessageService.Services.MessageService;

namespace ConnectHub.MessageService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("ERROR: Database connection string 'DefaultConnection' is missing! Using dummy to prevent DI crash.");
                connectionString = "Host=localhost;Database=Dummy;Username=dummy;Password=dummy";
            }
            else 
            {
                Console.WriteLine("MessageService: Connection string found (length: " + connectionString.Length + ")");
            }



            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory_Message")));

            var jwtSecret = builder.Configuration["JWT:Secret"];
            if (string.IsNullOrEmpty(jwtSecret))
                jwtSecret = "This-Is-My-Super-Secret-Key-For-JWT-At-Least-32-Characters-Long-ChangeThis!";

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
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                                context.Token = accessToken;
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddScoped<IMessageRepository, MessageRepository>();
            builder.Services.AddScoped<IMessageService, MessageServiceImpl>();
            builder.Services.AddSignalR();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ConnectHub Message API", Version = "v1" });
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
                    policy.WithOrigins("http://localhost:4200", "http://localhost:3000", "http://localhost:5000")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHub<ChatHub>("/chatHub");

            using (var scope = app.Services.CreateScope())
            {
                try 
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    
                    Console.WriteLine("MessageService: Syncing migration history...");
                    var syncSql = @"
                        CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory_Message"" (
                            ""MigrationId"" character varying(150) NOT NULL,
                            ""ProductVersion"" character varying(32) NOT NULL,
                            CONSTRAINT ""PK___EFMigrationsHistory_Message"" PRIMARY KEY (""MigrationId"")
                        );
                        INSERT INTO ""__EFMigrationsHistory_Message"" (""MigrationId"", ""ProductVersion"")
                        VALUES 
                        ('20240320145622_InitialCreate', '8.0.0'),
                        ('20240428184512_AddRoomIdToMessages', '8.0.0'),
                        ('20240428193045_AddReplyToMessageId', '8.0.0')
                        ON CONFLICT (""MigrationId"") DO NOTHING;
                    ";
                    await dbContext.Database.ExecuteSqlRawAsync(syncSql);

                    Console.WriteLine("Applying database migrations for MessageService...");
                    await dbContext.Database.MigrateAsync();
                    Console.WriteLine("MessageService: Database migration completed.");
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("already exists"))
                        Console.WriteLine("MessageService: Schema already exists, skipping migration conflict.");
                    else
                        Console.WriteLine($"CRITICAL ERROR: MessageService migration failed: {ex.Message}");
                }
            }

            Console.WriteLine("Message Service running! Swagger: http://localhost:5003/swagger");
            await app.RunAsync();
        }
    }
}