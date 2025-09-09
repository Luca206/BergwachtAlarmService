using AlarmService.Settings;
using Microsoft.Extensions.Options;
using Serilog;

namespace AlarmService.Services;

using Bwb.GraphQL.Client;
using global::AlarmService.Globals;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;

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
    /// Gets or sets the HttpClient used to access the GraphQL API.
    /// This client is configured with the GraphQL endpoint and any necessary authentication headers.
    /// </summary>
    private HttpClient HttpClient { get; set; }

    private GraphQLHttpClientOptions GraphQlOptions { get; set; }
    
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
    
    public GraphQlQueryService(HttpClient httpClient, ILogger<GraphQlQueryService> logger, IOptions<AlarmSettings> alarmSettings)
    {
        this.Logger = logger;
        this.AlarmSettings = alarmSettings.Value;
        
        this.HttpClient = httpClient;
        this.GraphQlOptions = new GraphQLHttpClientOptions()
        {
            EndPoint = new Uri(CompanionConstants.GraphQlUrl)
        };
    }
    
    public virtual string BuildQuery()
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
    
    public async Task<IReadOnlyCollection<Alarm>> GetAllAlarmsAsync()
    {
        var timestamp = DateTime.UtcNow.AddSeconds(- this.AlarmSettings.IntervalToCheckForAlarmsInSec);
        
        var operationName = "getAlarms";
        var builder = new RootQueryTypeQueryBuilder(operationName)
            .WithAlarms(
                pageOfAlarmQueryBuilder: new PageOfAlarmQueryBuilder()
                    .WithAllFields()
                    .WithAllScalarFields()
                    .WithResults(
                        new AlarmQueryBuilder()
                            .WithExtid()
                            .WithBwbOfficeId()
                            .WithSubkind()
                            .WithOriginatedAt()
                            .WithPayload()
                            .WithTypeName()),
                new AlarmFilterInput()
                {
                    OriginatedAt = new AlarmFilterOriginatedAt()
                    {
                        GreaterThanOrEqual = timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    },
                    Subkind = new AlarmFilterSubkind()
                    {
                        NotEq = "CLOSED",
                    },
                }).Build(Formatting.Indented);
        
        if (string.IsNullOrEmpty(builder))
        {
            return Array.Empty<Alarm>();
        }
        
        var response = await this.SendGraphQlAsync<IReadOnlyCollection<Alarm>>(builder, operationName);
        
        return response.Data;
    }
    
    public async Task<GraphQLResponse<T>> SendGraphQlAsync<T>(string builder, string operationName)
    {
        var graphQlClient = new GraphQLHttpClient(GraphQlOptions, new SystemTextJsonSerializer(), this.HttpClient);

        var userRequest = new GraphQLRequest
        {
            Query = builder,
            OperationName = operationName
        };

        var response = await graphQlClient.SendQueryAsync<T>(userRequest);
        
        if (response.Errors is not null 
            && response.Errors.Length > 0)
        {
            var messages = response.Errors.Select(e => e.Message);
            var allMessages = string.Join("; ", messages);
            throw new InvalidOperationException($"GraphQL errors: {allMessages}");
        }
        
        return response;
    }
}