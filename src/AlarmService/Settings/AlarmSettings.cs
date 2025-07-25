namespace AlarmService.Settings;

public class AlarmSettings
{
    /// <summary>
    /// Gets or sets the request interval to check for alarms in seconds.
    /// </summary>
    public int RequestIntervalInSec { get; set; }
    
    /// <summary>
    /// Gets or sets the interval of the requests in which the service checks for new alarms in seconds.
    /// </summary>
    public int IntervalToCheckForAlarmsInSec { get; set; }
    
    /// <summary>
    /// Gets or sets the time in seconds to keep the monitor turned on after an alarm is detected.
    /// </summary>
    public int KeepMonitorTurnedOnInSec { get; set; }

    /// <summary>
    /// Gets the interval of the requests in milliseconds.
    /// </summary>
    /// <returns></returns>
    public int GetRequestIntervalInMilliseconds()
    {
        return this.RequestIntervalInSec * 1000;
    }
}