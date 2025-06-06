using System.Text;
using System.Text.Json;
using AlarmService.Globals;

namespace AlarmService.Services;

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
    private GraphQlQueryService QueryService { get; }

    /// <summary>
    /// Gets or sets the HTTP request service that is responsible for sending HTTP requests to the companion service.
    /// </summary>
    private HttpRequestService HttpRequestService { get; }
    
    /// <summary>
    /// Gets or sets the service to filter alarms based on the configured criteria.
    /// </summary>
    private AlarmFilterService AlarmFilterService { get; }

    public CompanionService(
        ILogger<CompanionService> logger,
        GraphQlQueryService queryService,
        HttpRequestService httpRequestService,
        AlarmFilterService alarmFilterService)
    {
        this.Logger = logger;
        this.QueryService = queryService;
        this.HttpRequestService = httpRequestService;
        this.AlarmFilterService = alarmFilterService;
    }

    /// <summary>
    /// Checks if an alarm is detected by sending a GraphQL request to the companion service.
    /// </summary>
    /// <returns>A task symbolizing an async operation with a boolean value indicating whether a task was detected or not.</returns>
    public async Task<bool> CheckForAlarms()
    {
        this.Logger.LogDebug("Checking for alarms...");

        var query = this.QueryService.BuildQuery();

        var responseMessage = await this.RequestGraphQlAsync(query).ConfigureAwait(false);

        if (string.IsNullOrEmpty(responseMessage))
        {
            return false;
        }

        this.Logger.LogTrace(responseMessage);

        var responseJson = JsonDocument.Parse(responseMessage);

        if (!this.ValidateJson(responseJson, out var resultsElement))
        {
            this.Logger.LogWarning("Unexpected JSON structure in response.");
            return false;
        }
        
        if (resultsElement.GetArrayLength() == 0)
        {
            this.Logger.LogInformation("No alarms detected.");
            return false;
        }
        
        this.Logger.LogDebug("Found {Count} alarms in the response.", resultsElement.GetArrayLength());
        this.Logger.LogDebug("Filtering alarms based on criteria...");
        var alarms = this.AlarmFilterService.FilterAlarmsOfJsonElement(resultsElement);
        if (!alarms.Any())
        {
            this.Logger.LogDebug("No Alarms match the filter criteria.");
            return false;
        }
        this.Logger.LogInformation("Alarms detected!");
        this.Logger.LogDebug("Number of alarms matching the filter criteria: {Count}", alarms.Count);
        foreach (var alarm in alarms)
        {
            this.Logger.LogInformation($"Alarm detected: {alarm.Id}");
        }
        return true;
    }

    /// <summary>
    /// Request the GraphQL endpoint of the companion service with the given query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <returns>A task symbolizing an asynchronous operation with a sting containing the HttpResponse content if the request was successful or null.</returns>
    private async Task<string?> RequestGraphQlAsync(string query)
    {
        // build HTTP request to the companion service
        var content = new StringContent(query, Encoding.UTF8, "application/json");
        var httpResponse = await this.HttpRequestService.PostAsync(ApiEndpoints.GraphQl, null, content);

        if (!httpResponse.IsSuccessStatusCode)
        {
            this.Logger.LogWarning("Failed to send GraphQL request. Status code: {StatusCode}",
                httpResponse.StatusCode);
            return null;
        }

        return await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Validates the JSON response from the GraphQL request. If the response has not the expected structure,
    /// the response was not correct.
    /// </summary>
    /// <param name="json">The GraphQl Content as JsonDocument.</param>
    /// <param name="resultsElement">Out value. The Element of the results-array.</param>
    /// <returns>A boolean value indicating whether the response is valid or not.</returns>
    private bool ValidateJson(JsonDocument json, out JsonElement resultsElement)
    {
        resultsElement = new JsonElement();
        if (!(json.RootElement.TryGetProperty("data", out var dataElement) &&
              dataElement.ValueKind == JsonValueKind.Object))
        {
            // Log an error if the 'alarms' property is not found or is not an object.
            this.Logger.LogWarning("Unexpected response format: 'data' property not found or not an object.");
            return false;
        }

        if (!(dataElement.TryGetProperty("alarms", out var alarmsElement) &&
              alarmsElement.ValueKind == JsonValueKind.Object))
        {
            // Log an error if the 'alarms' property is not found or is not an object.
            this.Logger.LogWarning("Unexpected response format: 'alarms' property not found or not an object.");
            return false;
        }

        // Check if the 'alarms' property contains a 'results' array with at least one element.
        if (!(alarmsElement.TryGetProperty("results", out resultsElement) &&
              resultsElement.ValueKind == JsonValueKind.Array))
        {
            // Log an error if the 'results' property is not found or is not an array.
            this.Logger.LogWarning("Unexpected response format: 'results' property not found or not an array.");
            return false;
        }

        return true;
    }
}