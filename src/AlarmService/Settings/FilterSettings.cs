using System.Text.Json.Serialization;

namespace AlarmService.Settings;

public class FilterSettings
{
    /// <summary>
    /// Gets or sets the file path to the filter configuration file.
    /// </summary>
    public string FilterFilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets a collection of filter entries that define the filtering logic.
    /// </summary>
    [JsonPropertyName("Entries")]
    public IReadOnlyList<FilterEntry> FilterEntries { get; set; } = [];
}