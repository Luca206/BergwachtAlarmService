namespace AlarmService.Services;

using Dtos;
using Bwb.GraphQL.Client;
using Settings;
using Microsoft.Extensions.Options;

/// <summary>
/// Class for the companion service. This service is responsible for detecting an incoming alarm.
/// </summary>
public class CompanionService
{
  /// <summary>
  /// Gets or sets the logger for the <see cref="CompanionService"/>.
  /// </summary>
  private ILogger<CompanionService> Logger { get; }

  /// <summary>
  /// Gets or sets the GraphQL query service that is responsible for building GraphQL queries.
  /// </summary>
  private GraphQlQueryService GraphQlQueryService { get; }

  /// <summary>
  /// Gets or sets the settings for filtering alarms.
  /// </summary>
  private FilterSettings FilterSettings { get; set; }

  /// <summary>
  /// Gets or sets the settings for alarms.
  /// </summary>
  private AlarmSettings AlarmSettings { get; set; }

  public CompanionService(
    ILogger<CompanionService> logger,
    GraphQlQueryService graphQlQueryService,
    IOptions<FilterSettings> filterSettings,
    IOptions<AlarmSettings> alarmSettings)
  {
    this.Logger = logger;
    this.GraphQlQueryService = graphQlQueryService;
    this.AlarmSettings = alarmSettings.Value;
    this.FilterSettings = filterSettings.Value;

  }

  /// <summary>
  /// Checks if an alarm is detected by sending a GraphQL request to the companion service.
  /// </summary>
  /// <returns>A task symbolizing an async operation with a boolean value indicating whether a task was detected or not.</returns>
  public async Task<IReadOnlyCollection<DetectedAlarm>> CheckForAlarms()
  {
    this.Logger.LogTrace("Checking for alarms...");
    var allAlarms = await this.GraphQlQueryService.GetAllAlarmsAsync();

    if (allAlarms.Count == 0)
    {
      this.Logger.LogDebug("No alarms detected.");
      return Array.Empty<DetectedAlarm>();
    }

    // filter alarms
    var filteredAlarms = this.FilterAlarms(allAlarms);

    if (filteredAlarms.Count == 0)
    {
      this.Logger.LogDebug("No alarms detected.");
      return Array.Empty<DetectedAlarm>();
    }
    return filteredAlarms.Select(a => this.ConvertToDetectedAlarm(a))
      .Where(a => a is not null)
      .Select(a => a)
      .Cast<DetectedAlarm>()
      .ToList()
      .AsReadOnly();
  }

  private DetectedAlarm? ConvertToDetectedAlarm(Alarm alarm)
  {
    int id;
    if (!int.TryParse(alarm.Id, out id))
    {
      this.Logger.LogWarning("The 'id' property is not an integer.");
      return null;
    }

    var alarmTimestamp = DateTime.Parse(alarm.OriginatedAt.ToString());

    return new DetectedAlarm()
    {
      Id = id,
      AlarmTime = alarmTimestamp,
      ExpirationTime = alarmTimestamp.AddSeconds(this.AlarmSettings.KeepMonitorTurnedOnInSec),
      ReceivedTime = DateTime.UtcNow,
    };
  }

  private IReadOnlyCollection<Alarm> FilterAlarms(IReadOnlyCollection<Alarm> allAlarms)
  {
    this.Logger.LogDebug("Filtering alarms based on criteria...");
    return allAlarms;
  }
}