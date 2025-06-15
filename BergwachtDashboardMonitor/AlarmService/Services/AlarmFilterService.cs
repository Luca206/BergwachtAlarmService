using System.Text.Json;
using AlarmService.Dtos;
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

    public IReadOnlyCollection<DetectedAlarm> FilterAlarmsOfJsonElement(JsonElement jsonElement)
    {
        var detectedAlarms = new List<DetectedAlarm>();
        foreach (var alarmsElement in jsonElement.EnumerateArray())
        {
            if (this.FilterElements(alarmsElement, this.FilterSettings.Includes))
            {
                this.Logger.LogTrace("Found alarm that matches include criteria");
                var alarmToAdd = this.CreateDetectedAlarmFromJsonElement(alarmsElement);
                if (alarmToAdd is not null)
                {
                    detectedAlarms.Add(alarmToAdd);
                }
                else
                {
                    this.Logger.LogError("Failed to create DetectedAlarm from JSON element.");
                }
            }

            if (this.FilterElements(alarmsElement, this.FilterSettings.Excludes))
            {
                this.Logger.LogTrace("Found alarm that matches exclude criteria");
                var detectedAlarm = this.CreateDetectedAlarmFromJsonElement(alarmsElement);

                if (detectedAlarm is not null)
                {
                    detectedAlarms.Where(obj => obj.Id == detectedAlarm.Id)
                        .ToList()
                        .ForEach(obj => detectedAlarms.Remove(obj));
                }
                else
                {
                    this.Logger.LogError("Failed to create DetectedAlarm from JSON element.");
                }
            }
        }

        return detectedAlarms.AsReadOnly();
    }

    /// <summary>
    /// Filters the elements of a JSON element based on the provided filter criteria.
    /// </summary>
    /// <param name="alarm">The alarm to filter.</param>
    /// <param name="filterCriteriaList">A collection with the filter criteria.</param>
    /// <returns>A boolean value indicating whether a alarm was found matching the filter or not.</returns>
    private bool FilterElements(JsonElement alarm, ICollection<FilterCriteria> filterCriteriaList)
    {
        foreach (var filterCriteria in filterCriteriaList)
        {
            var steps = filterCriteria.Property.Split('.');
            var temp = alarm;
            foreach (var step in steps)
            {
                if (temp.TryGetProperty(step, out var value))
                {
                    temp = value;
                }
                else
                {
                    this.Logger.LogDebug($"Property '{step}' not found in alarm data.");
                    return false;
                }
            }

            if (temp.ValueKind == JsonValueKind.String
                && temp.GetString() == filterCriteria.Value)
            {
                this.Logger.LogDebug(
                    $"Alarm matches criteria: \"{filterCriteria.Property} = {filterCriteria.Value}\"");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Creates a <see cref="DetectedAlarm"/> from a <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="jsonElement">The <see cref="JsonElement"/> to convert.</param>
    /// <returns>The converted <see cref="DetectedAlarm"/>.</returns>
    private DetectedAlarm? CreateDetectedAlarmFromJsonElement(JsonElement jsonElement)
    {
        int id;
        
        if (jsonElement.TryGetProperty("id", out var idElement)
            && idElement.ValueKind == JsonValueKind.String)
        {
            var idString = idElement.GetString();
            if (string.IsNullOrEmpty(idString))
            {
                this.Logger.LogWarning("Alarm ID is null or empty.");
                return null;
            }
            id = int.Parse(idString);
        }
        else
        {
            return null;
        }

        var alarmTimeStamp = jsonElement.GetProperty("originatedAt").GetDateTime();
        
        return new DetectedAlarm
        {
            Id = id,
            AlarmTime = alarmTimeStamp,
            ReceivedTime = DateTime.UtcNow, // Assuming the received time is now
            ExpirationTime = alarmTimeStamp.AddSeconds(this.AlarmSettings.KeepMonitorTurnedOnInSec)
        };
    }
}