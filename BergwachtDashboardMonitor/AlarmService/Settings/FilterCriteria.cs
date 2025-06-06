namespace AlarmService.Settings;

public class FilterCriteria
{
    /// <summary>
    /// Gets or sets the property to filter on.
    /// </summary>
    public required string Property { get; set; }
    
    /// <summary>
    /// Gets or sets the value to filter by.
    /// </summary>
    public required string Value { get; set; }
}