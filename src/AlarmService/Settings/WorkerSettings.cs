namespace AlarmService.Settings;

public class WorkerSettings
{
    /// <summary>
    /// Gets or sets the time, the worker service sleeps between each execution.
    /// Default is 60s.
    /// </summary>
    public int RequestApiForNewAlarmsEverySec { get; set; } = 60;

    /// <summary>
    /// Gets or sets the interval, the worker checks for new alarms in the api.
    /// Default is 7200s => 2h.
    /// </summary>
    public int IntervalToCheckForAlarmsInSec { get; set; } = 7200;

    /// <summary>
    /// Gets or sets the time the monitor keeps turned on.
    /// Default is 1800s => 30min.
    /// </summary>
    public int KeepMonitorTurnedOnInSec { get; set; } = 1800;
    

    /// <summary>
    /// Gets the interval of the requests in milliseconds.
    /// </summary>
    /// <returns></returns>
    public int GetRequestIntervalInMilliseconds()
    {
        return this.RequestApiForNewAlarmsEverySec * 1000;
    }
}