using System.Diagnostics;
using AlarmService.Dtos;
using AlarmService.Services.Interfaces;
using AlarmService.Settings;
using Microsoft.Extensions.Options;

namespace AlarmService.Services;

/// <summary>
/// The WorkerService is a background service that periodically checks for alarms and controls the LG webOS TV
/// using the LgWebOsService to turn the display on/off based on alarm activity.
/// </summary>
public class WorkerService : BackgroundService
{
    /// <summary>
    /// List that stores all current active alarms.
    /// </summary>
    private List<DetectedAlarm> currentActiveAlarms = new();

    /// <summary>
    /// Gets or sets the logger for the <see cref="WorkerService"/>.
    /// </summary>
    private ILogger<WorkerService> Logger { get; set; }

    /// <summary>
    /// Gets or sets the alarm settings.
    /// </summary>
    private WorkerSettings WorkerSettings { get; set; }

    /// <summary>
    /// Gets or sets the companion settings that are used to connect to the companion service.
    /// </summary>
    private DashboardSettings DashboardSettings { get; set; }

    private TvSettings TvSettings { get; set; }

    /// <summary>
    /// Gets or sets the browser service that is responsible for checking if the expected URL is open.
    /// </summary>
    private BrowserService BrowserService { get; set; }

    /// <summary>
    /// Gets or sets the LG webOS service that is responsible for controlling the TV.
    /// </summary>
    private ITvService LgTvService { get; set; }

    private AlarmService AlarmService { get; }

    public WorkerService(
        ILogger<WorkerService> logger,
        IOptions<WorkerSettings> settings,
        IOptions<DashboardSettings> dashboardSettings,
        IOptions<TvSettings> tvSettings,
        AlarmService alarmService,
        BrowserService browserService,
        ITvService lgTvService)
    {
        this.Logger = logger;
        this.WorkerSettings = settings.Value;
        this.DashboardSettings = dashboardSettings.Value;
        this.TvSettings = tvSettings.Value;
        this.AlarmService = alarmService;
        this.BrowserService = browserService;
        this.LgTvService = lgTvService;
    }

    /// <summary>
    /// Executes the worker service
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await this.SetupWorkerService(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var activeAlarms = await this.AlarmService.GetAlarms();

            if (activeAlarms.Count > 0)
            {
                this.currentActiveAlarms.AddRange(activeAlarms);
                if (!await this.LgTvService.IsScreenOnAsync(stoppingToken))
                {
                    await this.LgTvService.TurnOnAsync(stoppingToken);
                }
            }

            // check if tv has to be turned off
            this.currentActiveAlarms.RemoveAll(a => a.ExpirationTime < DateTime.Now);

            if (this.currentActiveAlarms.Count == 0
                && await this.LgTvService.IsScreenOnAsync(stoppingToken))
            {
                await this.LgTvService.TurnOffAsync(stoppingToken);
            }

            this.Logger.LogInformation("Sleeping for {Interval}ms", this.WorkerSettings.GetRequestIntervalInMilliseconds());
            Thread.Sleep(this.WorkerSettings.GetRequestIntervalInMilliseconds());
        }
    }

    private async Task SetupWorkerService(CancellationToken stoppingToken)
    {
        if (this.DashboardSettings.UseBrowserService)
        {
            if (!await this.BrowserService.IsExpectedUrlOpenAsync(
                    new Uri(DashboardSettings.BaseUrl, DashboardSettings.DashboardPath).ToString()))
            {
                this.Logger.LogInformation("Expected URL is not open, starting dashboard.");


                this.StartDashboard();
            }
        }
        else
        {
            this.Logger.LogInformation("Browser service is disabled, skipping check for expected URL.");
        }

        await this.LgTvService.ConnectAsync(this.TvSettings.IpAddress, stoppingToken);
    }

    private void StartDashboard()
    {
        try
        {
            var dashboardUrl = new Uri(this.DashboardSettings.BaseUrl, DashboardSettings.DashboardPath);
            var authUrl = $"{DashboardSettings.AuthenticationUrl}{DashboardSettings.AccessToken}";

            var url = $"{authUrl}&redirect={dashboardUrl}?dashboardState={DashboardSettings.DashboardToken}";

            Process.Start("bash", $"-c \"chromium --app={url} --start-fullscreen\"");
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error while launching the Bergwacht Companion Dashboard.");
        }
    }
}