namespace AlarmService.Settings;

public class CompanionSettings
{
    /// <summary>
    /// Gets or sets the base URL for the companion service.
    /// </summary>
    public string? BaseUrl { get; set; }
    
    /// <summary>
    /// Gets or sets the access token for the companion service.
    /// </summary>
    public string? AccessToken { get; set; }
    
    /// <summary>
    /// Gets or sets the dashboard token for the companion service.
    /// </summary>
    public string? DashboardToken { get; set; }
    
    public Uri GetBaseUri()
    {
        if (string.IsNullOrEmpty(BaseUrl))
        {
            throw new InvalidOperationException("BaseUrl is not set.");
        }

        return new Uri(BaseUrl);
    }
}