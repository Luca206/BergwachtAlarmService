using System.Net.Http.Headers;
using AlarmService.Dtos;
using AlarmService.Settings;
using Microsoft.Extensions.Options;
using Serilog;
using Bwb.GraphQL.Client;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;

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
    private WorkerSettings WorkerSettings { get; set; }
    
    private GraphQlSettings GraphQlSettings { get; set; }

    /// <summary>
    /// Gets or sets the HttpClient used to access the GraphQL API.
    /// This client is configured with the GraphQL endpoint and any necessary authentication headers.
    /// </summary>
    private HttpClient HttpClient { get; set; }

    private GraphQLHttpClientOptions GraphQlOptions { get; set; }
    
    public GraphQlQueryService(
        HttpClient httpClient,
        ILogger<GraphQlQueryService> logger,
        IOptions<WorkerSettings> workerSettings,
        IOptions<GraphQlSettings> graphQlSettings)
    {
        this.Logger = logger;
        this.WorkerSettings = workerSettings.Value;
        this.GraphQlSettings = graphQlSettings.Value;
        
        this.HttpClient = httpClient;
        this.HttpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", this.GraphQlSettings.AccessToken);
        this.GraphQlOptions = new GraphQLHttpClientOptions()
        {
            EndPoint = this.GraphQlSettings.BaseUrl
        };
    }

    public async Task<IReadOnlyCollection<Alarm>> GetAllAlarmsAsync()
    {
        var timestamp = DateTime.UtcNow.AddSeconds(-this.WorkerSettings.IntervalToCheckForAlarmsInSec);

        var builder = new RootQueryTypeQueryBuilder(OperationName)
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
                originatedAfter: timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                filter: new AlarmFilterInput()
                {
                    Subkind = new AlarmFilterSubkind()
                    {
                        NotEq = "CLOSED",
                    },
                },
                offset: 0,
                limit: 250).Build(Formatting.Indented);
        
        if (string.IsNullOrEmpty(builder))
        {
            return Array.Empty<Alarm>();
        }
        
        var response = await this.SendGraphQlAsync<GetAlarmsResponse>(builder, OperationName);
        if (response.Data.Alarms.Results.Count == 0)
        {
            return Array.Empty<Alarm>();
        }

        return response.Data.Alarms.Results;
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