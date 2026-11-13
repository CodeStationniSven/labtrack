using LabTrack.Application.Interfaces;
using LabTrack.Api.Controllers;

namespace LabTrack.Api.Workers;

public sealed class NotificationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public NotificationWorker(
        IServiceProvider serviceProvider,
        ILogger<NotificationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationWorker background service has started.");

        using var timer = new PeriodicTimer(_checkInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessPendingNotificationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing system notifications.");
            }
        }

        _logger.LogInformation("NotificationWorker background service is stopping.");
    }

    private async Task ProcessPendingNotificationsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var notificationService = scope.ServiceProvider.GetService<INotificationService>();

        if (notificationService is null)
        {
            _logger.LogTrace("INotificationService is not registered or available for background notification dispatch.");
            return;
        }

        _logger.LogTrace("Scanning for queued system alert notifications...");
        
        // Simulating notification queue processing and broadcast via INotificationService
        await Task.CompletedTask;
    }
}

