namespace AlarmService.Dtos;

public class DetectedAlarm
{
    /// <summary>
    /// Gets or sets the unique identifier for the alarm.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp of the alarm in UTC.
    /// </summary>
    public DateTime AlarmTime { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the alarm was received.
    /// </summary>
    public DateTime ReceivedTime { get; set; }
}