namespace AlarmService.Settings;

public class FilterSettings
{
    /// <summary>
    /// Gets or sets the list of filter criteria to include.
    /// </summary>
    public List<FilterCriteria> Includes { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the list of filter criteria to exclude.
    /// </summary>
    public List<FilterCriteria> Excludes { get; set; } = new();
}