using AlarmService.Settings;
using Microsoft.Extensions.Options;

namespace AlarmService.Services;

/// <summary>
/// The WorkerService is a background service that periodically checks for alarms
/// </summary>
public class WorkerService : BackgroundService
{
    
    /// <summary>
    /// Gets or sets the logger for the <see cref="WorkerService"/>.
    /// </summary>
    private ILogger<WorkerService> Logger { get; set; }
    
    /// <summary>
    /// Gets or sets the alarm settings.
    /// </summary>
    private AlarmSettings Settings { get; set; }
    
    /// <summary>
    /// Gets or sets the companion service that is responsible for detecting alarms.
    /// </summary>
    private CompanionService CompanionService { get; set; }

    /// <summary>
    /// Gets or sets the CEC service that is responsible for handling CEC commands.
    /// </summary>
    private CecService CecService { get; set; }
    
    public WorkerService(
        ILogger<WorkerService> logger, 
        IOptions<AlarmSettings> settings,
        CompanionService companionService,
        CecService cecService)
    {
        this.Logger = logger;
        this.Settings = settings.Value;
        this.CompanionService = companionService;
        this.CecService = cecService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var alarmDetected = await this.CompanionService.CheckForAlarms().ConfigureAwait(false);

            if (alarmDetected)
            {
                this.Logger.LogDebug("Alarm detected!");
            }
            else
            {
                this.Logger.LogDebug("No Alarm detected!");
            }
            
            await Task.Delay(this.Settings.GetRequestIntervalInMilliseconds(), stoppingToken);
        }
    }
}