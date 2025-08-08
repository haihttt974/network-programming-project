using DKyThucTap.Services;

namespace DKyThucTap.Services
{
    public class OnlineUserCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OnlineUserCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(2); // Cleanup every 2 minutes

        public OnlineUserCleanupService(IServiceProvider serviceProvider, ILogger<OnlineUserCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Online User Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var onlineUserService = scope.ServiceProvider.GetRequiredService<IOnlineUserService>();
                        await onlineUserService.CleanupInactiveConnectionsAsync();
                    }

                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Online User Cleanup Service");
                    
                    // Wait a bit before retrying to avoid tight loop on persistent errors
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            _logger.LogInformation("Online User Cleanup Service stopped");
        }
    }
}
