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
    private AlarmSettings AlarmSettings { get; set; }

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
        this.AlarmSettings = settings.Value;
        this.CompanionService = companionService;
        this.CecService = cecService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var detectedAlarms = await this.CompanionService.CheckForAlarms().ConfigureAwait(false);

            if (detectedAlarms is not null && detectedAlarms.Any())
            {
                this.Logger.LogInformation("Detected {Count} alarms.", detectedAlarms.Count);
                var activeAlarm = false;
                foreach (var alarm in detectedAlarms)
                {
                    this.Logger.LogInformation("Alarm: {Alarm}", alarm);

                    if (alarm.ExpirationTime.CompareTo(DateTime.Now).Equals(1))
                    {
                        this.Logger.LogDebug("Alarm is still active, turning on monitor if not already on.");
                        activeAlarm = true;
                        if (!this.CecService.IsMonitorOn)
                        {
                            this.CecService.TurnOn();
                        }
                    }

                    if (!activeAlarm && this.CecService.IsMonitorOn)
                    {
                        this.Logger.LogDebug("Alarm expired, turning off monitor.");
                        this.CecService.TurnOff();
                    }
                }
            }
            else
            {
                this.Logger.LogInformation("No alarms detected.");
            }

            await Task.Delay(this.AlarmSettings.GetRequestIntervalInMilliseconds(), stoppingToken);
        }
    }
}