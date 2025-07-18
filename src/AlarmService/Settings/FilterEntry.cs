using System.Text.Json.Serialization;

namespace AlarmService.Settings;

public class FilterEntry
{
    /// <summary>
    /// Gets or sets the property path to filter on.
    /// </summary>
    [JsonPropertyName("PropertyPath")]
    public required string PropertyPath { get; set; }
    
    [JsonPropertyName("Value")]
    public required string Value { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this filter entry is an inclusion or exclusion.
    /// </summary>
    [JsonPropertyName("IncludeResult")]
    public required bool IsInclude { get; set; }
}