using System.Net.Http.Json;

namespace AlarmService.Services;

public class BrowserService
{
    /// <summary>
    /// Gets or sets the logger for the <see cref="AlarmFilterService"/>.
    /// </summary>
    private ILogger<BrowserService> Logger { get; set; }
    
    
    private HttpClient HttpClient { get; set; }
    
    public BrowserService(ILogger<BrowserService> logger, HttpClient httpClient)
    {
        this.Logger = logger;
        this.HttpClient = httpClient;
    }
    
    public async Task<bool> IsExpectedUrlOpenAsync(string expectedUrl)
    {
        try
        {
            var tabs = await HttpClient.GetFromJsonAsync<List<ChromiumTab>>("https://pages.services.bergwacht-bayern.org/dashboard");
            if (tabs == null)
            {
                return false;
            }

            return tabs.Any(tab => tab.Url.StartsWith(expectedUrl, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error while checking for expected URL: {ex.Message}");
            return false;
        }
    }

    private record ChromiumTab(string Id, string Title, string Url);
}