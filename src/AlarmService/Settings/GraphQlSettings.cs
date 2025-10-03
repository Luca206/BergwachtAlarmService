namespace AlarmService.Settings;

public class GraphQlSettings
{
    /// <summary>
    /// Gets or sets the base URL of the GraphQL API.
    /// </summary>
    public Uri BaseUrl { get; set; }
    
    /// <summary>
    /// Gets or sets the access token for authentication.
    /// </summary>
    public string AccessToken { get; set; }
}