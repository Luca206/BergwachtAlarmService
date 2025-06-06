namespace AlarmService.Settings;

public class AlarmSettings
{
    /// <summary>
    /// Gets or sets the interval checked for alarms in seconds.
    /// </summary>
    public int AlarmCheckInterval { get; set; }
    
    /// <summary>
    /// Gets or sets the interval of the requests in seconds.
    /// </summary>
    public int RequestIntervalInSec { get; set; }
    
    /// <summary>
    /// Gets the interval of the requests in milliseconds.
    /// </summary>
    /// <returns></returns>
    public int GetRequestIntervalInMilliseconds()
    {
        return this.RequestIntervalInSec * 1000;
    }
}