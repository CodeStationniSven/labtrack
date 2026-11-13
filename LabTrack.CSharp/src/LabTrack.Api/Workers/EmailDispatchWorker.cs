using LabTrack.Application.Interfaces;
using LabTrack.Api.Controllers;

namespace LabTrack.Api.Workers;

public sealed class EmailDispatchWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailDispatchWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(15);

    public EmailDispatchWorker(
        IServiceProvider serviceProvider,
        ILogger<EmailDispatchWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailDispatchWorker background service has started.");

        using var timer = new PeriodicTimer(_checkInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessPendingEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing pending email dispatches.");
            }
        }

        _logger.LogInformation("EmailDispatchWorker background service is stopping.");
    }

    private async Task ProcessPendingEmailsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var communicationService = scope.ServiceProvider.GetService<ICommunicationService>();

        if (communicationService is null)
        {
            _logger.LogTrace("ICommunicationService is not registered or available for email dispatch.");
            return;
        }

        _logger.LogTrace("Checking for queued email notifications...");
        
        // Simulating queue processing logic via service call
        await Task.CompletedTask;
    }
}

