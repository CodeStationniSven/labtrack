using LabTrack.Application.Interfaces;
using LabTrack.Api.Controllers;

namespace LabTrack.Api.Workers;

public sealed class PcSessionHeartbeatWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PcSessionHeartbeatWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);

    public PcSessionHeartbeatWorker(
        IServiceProvider serviceProvider,
        ILogger<PcSessionHeartbeatWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PcSessionHeartbeatWorker background service has started.");

        using var timer = new PeriodicTimer(_checkInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessActiveSessionHeartbeatsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while evaluating active PC session heartbeats.");
            }
        }

        _logger.LogInformation("PcSessionHeartbeatWorker background service is stopping.");
    }

    private async Task ProcessActiveSessionHeartbeatsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var pcUsageService = scope.ServiceProvider.GetService<IPcUsageService>();

        if (pcUsageService is null)
        {
            _logger.LogTrace("IPcUsageService is not registered or available for PC session heartbeat validation.");
            return;
        }

        _logger.LogTrace("Scanning active workstation sessions for timeout or heartbeat expiration...");

        // Simulating periodic session validation and stale connection cleanup
        await Task.CompletedTask;
    }
}

