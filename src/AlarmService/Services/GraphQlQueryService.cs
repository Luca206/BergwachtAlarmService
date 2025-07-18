using AlarmService.Settings;
using Microsoft.Extensions.Options;
using Serilog;

namespace AlarmService.Services;

/// <summary>
/// Service to build GraphQL queries for alarms.
/// </summary>
public class GraphQlQueryService
{
    private const string OperationName = "GetAlarms";
    
    /// <summary>
    /// Gets or sets the logger for the <see cref="GraphQlQueryService"/>.
    /// </summary>
    private ILogger<GraphQlQueryService> Logger { get; set; }
    
    /// <summary>
    /// Gets or sets the settings for alarms.
    /// </summary>
    private AlarmSettings AlarmSettings { get; set; }
    
    /// <summary>
    /// Gets or sets the variables for the GraphQL query.
    /// </summary>
    private Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>
    {
        { "limit", 250 },
        { "offset", 0 },
        { "originatedAfter", "timestamp" },
        { "filter", new Dictionary<string, object>
            {
                { "subkind", new Dictionary<string, string> { { "NotEq", "CLOSED" } } }
            }
        }
    };
    
    /// <summary>
    /// Gets or sets the GraphQL query for fetching alarms.
    /// </summary>
    private string Query { get; set; } = $@"
        query {OperationName}($originatedAfter: DateTime, $search: String, $filter: AlarmFilterInput, $limit: Int = 250, $offset: Int = 0) {{
            alarms(
                search: $search
                sort: {{field: ORIGINATED_AT, order: DESC}}
                createdAfter: $originatedAfter
                filter: $filter
                limit: $limit
                offset: $offset
            ) {{
                hasNextPage
                results {{
                    id
                    subkind
                    originatedAt
                    payload
                    extid
                    __typename
                }}
                __typename
            }}
        }}";
    
    public GraphQlQueryService(ILogger<GraphQlQueryService> logger, IOptions<AlarmSettings> alarmSettings)
    {
        this.Logger = logger;
        this.AlarmSettings = alarmSettings.Value;
    }
    
    public string BuildQuery()
    {
        this.Logger.LogDebug("Building GraphQL query for alarms...");
        
        var timestamp = DateTime.UtcNow.AddSeconds(- this.AlarmSettings.IntervalToCheckForAlarmsInSec);
        Log.Information("Setting originatedAfter to {Timestamp}", timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        this.Variables["originatedAfter"] = timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        
        return $@"
        {{
            ""operationName"": ""{OperationName}"",
            ""variables"": {System.Text.Json.JsonSerializer.Serialize(this.Variables)},
            ""query"": ""{this.Query.Replace("\n", " ").Replace("\"", "\\\"")}""
        }}";
    }
}