using System.Text.Json;
using System.Text.Json.Nodes;
using AlarmService.Dtos;
using AlarmService.Extensions;
using AlarmService.Settings;
using Microsoft.Extensions.Options;

namespace AlarmService.Services;

public class AlarmFilterService
{
    /// <summary>
    /// Gets or sets the logger for the <see cref="AlarmFilterService"/>.
    /// </summary>
    private ILogger<AlarmFilterService> Logger { get; set; }

    /// <summary>
    /// Gets or sets the settings for filtering alarms.
    /// </summary>
    private FilterSettings FilterSettings { get; set; }

    /// <summary>
    /// Gets or sets the settings for alarms.
    /// </summary>
    private AlarmSettings AlarmSettings { get; set; }

    public AlarmFilterService(
        ILogger<AlarmFilterService> logger,
        IOptions<FilterSettings> filterSettings,
        IOptions<AlarmSettings> alarmSettings)
    {
        this.Logger = logger;
        this.FilterSettings = filterSettings.Value;
        this.AlarmSettings = alarmSettings.Value;
    }

    public virtual IReadOnlyCollection<DetectedAlarm> FilterAlarmsOfJsonElement(JsonElement detectedAlarmsJson)
    {
        this.FetchFilterSettings();
        var detectedAlarmsArray = detectedAlarmsJson.ToJsonArray();
        if (detectedAlarmsArray is not null)
        {
            var filteredAlarms = this.FilterJson(detectedAlarmsArray, this.FilterSettings.FilterEntries);
            return filteredAlarms;
        }
        else
        {
            this.Logger.LogError("Detected alarms JSON is not an array.");
            return Array.Empty<DetectedAlarm>();
        }
    }

    /// <summary>
    /// Filters a JSON array based on a collection of filter criteria.
    /// <para>
    /// Filter logic:<br/>
    /// - If an item matches any of the exclude criteria, it is excluded. In this case, no further checks are made for that item.<br/>
    /// - If an item matches any of the include criteria, it is included. In this case, no further checks are made for that item.<br/>
    /// - If no filter criteria are defined, all items are included by default.
    /// </para>
    /// </summary>
    /// <param name="detectedAlarms">JSON-Array, which contains all detected alarms.</param>
    /// <param name="filterEntryCollection">Collection with the filter entries.</param>
    /// <returns>Read-only collection containing all alarms matching the filters.</returns>
    public IReadOnlyCollection<DetectedAlarm> FilterJson(JsonArray detectedAlarms,
        IReadOnlyCollection<FilterEntry> filterEntryCollection)
    {
        var filteredItems = new List<DetectedAlarm>();

        foreach (var item in detectedAlarms)
        {
            if (item == null)
            {
                this.Logger.LogTrace("Skipping null item in JSON array.");
                continue;
            }

            // By default, the item is not included or excluded.
            var includeItem = false;
            var excludeItem = false;

            // check every filter entry for the current item
            foreach (var filterEntry in filterEntryCollection)
            {
                // Versuche, den Wert der Eigenschaft basierend auf dem PropertyPath zu erhalten.
                if (TryGetValueFromPath(item, filterEntry.PropertyPath, out string? propertyValue))
                {
                    if (string.Equals(propertyValue, filterEntry.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        if (filterEntry.IsInclude)
                        {
                            var logMessage =
                                $"Including item based on filter criteria: {filterEntry.PropertyPath} = {filterEntry.Value}.";
                            logMessage += " No further checks are needed.";
                            this.Logger.LogDebug(logMessage);
                            includeItem = true;
                        }
                        else
                        {
                            var logMessage =
                                $"Excluding item based on filter criteria: {filterEntry.PropertyPath} = {filterEntry.Value}.";
                            logMessage += " No further checks are needed.";
                            this.Logger.LogDebug(logMessage);
                            excludeItem = true;
                        }

                        break;
                    }
                }
            }

            if (excludeItem is false
                && (includeItem || !filterEntryCollection.Any()))
            {
                var detectedAlarm = this.CreateDetectedAlarmFromJsonNode(item);
                if (detectedAlarm is not null)
                {
                    filteredItems.Add(detectedAlarm);
                }
                else
                {
                    this.Logger.LogError("Failed to create DetectedAlarm from JSON node.");
                }
            }
        }

        return filteredItems.AsReadOnly();
    }

    /// <summary>
    /// Versucht, den Wert einer Eigenschaft aus einem JsonNode anhand eines PropertyPath zu extrahieren.
    /// Unterstützt Punktnotation für Objekte und Klammernotation für Arrays (z.B. "data.items[0].value").
    /// </summary>
    /// <param name="jsonNode">Der JsonNode, aus dem der Wert extrahiert werden soll.</param>
    /// <param name="path">Der Pfad zur Eigenschaft.</param>
    /// <param name="value">Der extrahierte Wert, wenn gefunden; sonst null.</param>
    /// <returns>True, wenn der Wert gefunden wurde; sonst false.</returns>
    public bool TryGetValueFromPath(JsonNode jsonNode, string path, out string? value)
    {
        value = null;
        var currentNode = jsonNode;
        var pathParts = path.Split('.');

        foreach (var part in pathParts)
        {
            if (part.Contains("[") && part.Contains("]"))
            {
                this.Logger.LogTrace($"PropertyPath contains array index: {part}");

                var arrayName = part.Substring(0, part.IndexOf("[", StringComparison.OrdinalIgnoreCase));
                var indexString = part.Substring(
                    part.IndexOf("[", StringComparison.OrdinalIgnoreCase) + 1,
                    part.IndexOf("]", StringComparison.OrdinalIgnoreCase) -
                    part.IndexOf("[", StringComparison.OrdinalIgnoreCase) - 1);

                if (!int.TryParse(indexString, out int index))
                {
                    this.Logger.LogWarning($"Invalid array index in PropertyPath \"{path}\": {indexString}");
                    return false;
                }

                if (currentNode is JsonObject objNode && objNode[arrayName] is JsonArray jsonArray)
                {
                    if (index >= 0 && index < jsonArray.Count)
                    {
                        var newNode = jsonArray[index];

                        if (newNode is null)
                        {
                            this.Logger.LogInformation(
                                $"Array index {index} is null for array \"{arrayName}\" in PropertyPath \"{path}\".");
                            return false;
                        }

                        currentNode = newNode;
                    }
                    else
                    {
                        this.Logger.LogDebug(
                            $"Array index {index} out of bounds for array \"{arrayName}\" in PropertyPath \"{path}\".");
                        return false;
                    }
                }
                else
                {
                    this.Logger.LogDebug(
                        $"Property \"{arrayName}\" not found in JsonObject or not an array in PropertyPath \"{path}\".");
                    return false;
                }
            }
            else
            {
                if (currentNode is JsonObject objNode)
                {
                    var newNode = objNode[part];
                    if (newNode is null)
                    {
                        this.Logger.LogInformation($"Property \"{part}\" not found in JsonObject.");
                        return false;
                    }

                    currentNode = newNode;
                }
                else
                {
                    return false;
                }
            }
        }

        if (currentNode is JsonValue jsonValue)
        {
            value = jsonValue.GetValue<string?>();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Convert a <see cref="JsonNode"/> to a <see cref="DetectedAlarm"/>.
    /// </summary>
    /// <param name="jsonNode">The Json node to convert.</param>
    /// <returns>The converted <see cref="DetectedAlarm"/>.</returns>
    public DetectedAlarm? CreateDetectedAlarmFromJsonNode(JsonNode jsonNode)
    {
        if (jsonNode is not JsonObject jsonObject)
        {
            this.Logger.LogWarning("The provided JsonNode is not a JsonObject.");
            return null;
        }

        // get id from the JSON object
        string? idString = jsonObject["id"]?.AsValue().ToString();
        if (string.IsNullOrWhiteSpace(idString))
        {
            this.Logger.LogError("The 'id' property is missing or empty in the JSON object.");
            return null;
        }

        int id;
        if (!int.TryParse(idString, out id))
        {
            this.Logger.LogWarning("The 'id' property is not an integer.");
            return null;
        }

        // get the 'originatedAt' property from the JSON object
        DateTime alarmTimeStamp;
        try
        {
            var originatedAtNode = jsonObject["originatedAt"];
            if (originatedAtNode == null)
            {
                this.Logger.LogWarning("The 'originatedAt' property is not in the provided JsonObject.");
                return null;
            }

            alarmTimeStamp = originatedAtNode.AsValue().GetValue<DateTime>();
        }
        catch (InvalidCastException ex)
        {
            this.Logger.LogError(ex,
                $"Error while converting 'originatedAt' to DateTime. Value in JSON: {jsonObject["originatedAt"]}");
            return null;
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Unexpected error while retrieving 'originatedAt' from JSON object.");
            return null;
        }

        return new DetectedAlarm
        {
            Id = id,
            AlarmTime = alarmTimeStamp,
            ReceivedTime = DateTime.UtcNow,
            ExpirationTime = alarmTimeStamp.AddSeconds(this.AlarmSettings.KeepMonitorTurnedOnInSec)
        };
    }

    private void FetchFilterSettings()
    {
        if (File.Exists(this.FilterSettings.FilterFilePath))
        {
            // Check if file is currently opened by another process
            try
            {
                var jsonContent = File.ReadAllText(this.FilterSettings.FilterFilePath);
                var rootJson = JsonSerializer.Deserialize<Dictionary<string, FilterSettings>>(jsonContent);

                if (rootJson is null || !rootJson.TryGetValue("Filter", out var filterSettings))
                {
                    this.Logger.LogError("Filter settings not found in the JSON file.");
                    return;
                }

                if (this.FilterSettingsUpdated(filterSettings))
                {
                    this.FilterSettings.FilterEntries = filterSettings.FilterEntries;
                    this.Logger.LogInformation("Filter settings updated successfully.");
                }
                else
                {
                    this.Logger.LogDebug("No changes in filter settings, keeping current settings.");
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogWarning(ex, "Error reading filter settings from file.");
            }
        }
        else
        {
            this.Logger.LogError($"Filter settings file not found at {this.FilterSettings.FilterFilePath}.");
        }
    }

    public bool FilterSettingsUpdated(FilterSettings filterSettings)
    {
        if (!filterSettings.FilterEntries.Any())
        {
            this.Logger.LogDebug("No filter entries found in the provided filter settings.");
            return false;
        }
        if (!this.FilterSettings.FilterEntries.Any())
        {
            this.Logger.LogDebug("Current filter settings have no entries, updating with new filter settings.");
            return true;
        }
        
        // Compare filterSettings.FilterEntries with this.FilterSettings.FilterEntries
        foreach (var entry in filterSettings.FilterEntries)
        {
            if (!this.FilterSettings.FilterEntries
                    .Any(e =>
                        e.PropertyPath.Equals(entry.PropertyPath)
                        && e.Value.Equals(entry.Value)))
            {
                this.Logger.LogDebug("Filter settings were updated.");
                return true;
            }
        }

        return false;
    }
}