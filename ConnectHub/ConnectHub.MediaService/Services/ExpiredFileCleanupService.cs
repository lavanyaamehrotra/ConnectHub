using ConnectHub.MediaService.Interfaces;

namespace ConnectHub.MediaService.Services
{
    // ============================================================
    // UC6 — ExpiredFileCleanupService
    // IHostedService that runs daily to delete expired files
    // from both Azure Blob Storage and the database.
    // ============================================================
    public class ExpiredFileCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory              _scopeFactory;
        private readonly ILogger<ExpiredFileCleanupService> _logger;

        public ExpiredFileCleanupService(
            IServiceScopeFactory               scopeFactory,
            ILogger<ExpiredFileCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ExpiredFileCleanupService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope      = _scopeFactory.CreateScope();
                    var mediaService     = scope.ServiceProvider
                        .GetRequiredService<IMediaService>();

                    await mediaService.CleanupExpiredFilesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during expired file cleanup");
                }

                // Run once every 24 hours
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
