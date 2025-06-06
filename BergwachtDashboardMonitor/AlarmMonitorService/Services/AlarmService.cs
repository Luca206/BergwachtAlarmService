namespace AlarmMonitorService.Services;

public class AlarmService : IHostedService
{
    private readonly ILogger<AlarmService> _logger;

    public AlarmService(ILogger<AlarmService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AlarmService gestartet.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AlarmService wird gestoppt.");
        return Task.CompletedTask;
    }
}