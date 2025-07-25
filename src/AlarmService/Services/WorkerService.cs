using System.Diagnostics;
using AlarmService.Globals;
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
    /// Gets or sets the companion settings that are used to connect to the companion service.
    /// </summary>
    private CompanionSettings CompanionSettings { get; set; }

    /// <summary>
    /// Gets or sets the companion service that is responsible for detecting alarms.
    /// </summary>
    private CompanionService CompanionService { get; set; }

    /// <summary>
    /// Gets or sets the browser service that is responsible for checking if the expected URL is open.
    /// </summary>
    private BrowserService BrowserService { get; set; }

    /// <summary>
    /// Gets or sets the CEC service that is responsible for handling CEC commands.
    /// </summary>
    private CecService CecService { get; set; }

    public WorkerService(
        ILogger<WorkerService> logger,
        IOptions<AlarmSettings> settings,
        IOptions<CompanionSettings> companionSettings,
        CompanionService companionService,
        BrowserService browserService,
        CecService cecService)
    {
        this.Logger = logger;
        this.AlarmSettings = settings.Value;
        this.CompanionSettings = companionSettings.Value;
        this.CompanionService = companionService;
        this.BrowserService = browserService;
        this.CecService = cecService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (await this.BrowserService.IsExpectedUrlOpenAsync(CompanionConstants.DashboardUrl) is false)
        {
            this.Logger.LogInformation("Expected URL is not open, starting dashboard.");
            this.StartDashboard();
        }

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
                        if (!await this.CecService.IsMonitorActive().ConfigureAwait(false))
                        {
                            this.Logger.LogInformation("Alarm is active and Monitor off, turning on monitor.");
                            this.CecService.TurnOn();
                        }
                    }

                    if (!activeAlarm && await this.CecService.IsMonitorActive().ConfigureAwait(false))
                    {
                        this.Logger.LogDebug("Alarm expired, turning off monitor.");
                        this.CecService.TurnOff();
                    }
                }
            }
            else
            {
                this.Logger.LogInformation("No alarms detected.");
                if (await this.CecService.IsMonitorActive().ConfigureAwait(false))
                {
                    this.Logger.LogDebug("No active alarms, turning off monitor.");
                    this.CecService.TurnOff();
                }
            }

            await Task.Delay(this.AlarmSettings.GetRequestIntervalInMilliseconds(), stoppingToken);
        }
    }

    private void StartDashboard()
    {
        try
        {
            var dashboardUrl =
                CompanionConstants.GetFullDashboardUrl(CompanionSettings.AccessToken, CompanionSettings.DashboardToken);
            Process.Start("bash", $"-c \"chromium --app={dashboardUrl} --start-fullscreen\"");
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error while launching the Bergwacht Companion Dashboard.");
        }
    }
}