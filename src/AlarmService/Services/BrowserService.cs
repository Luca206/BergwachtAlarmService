using System.Net.Http.Json;
using AlarmService.Settings;
using Microsoft.Extensions.Options;

namespace AlarmService.Services;

public class BrowserService
{
    /// <summary>
    /// Gets or sets the logger for the <see cref="AlarmFilterService"/>.
    /// </summary>
    private ILogger<BrowserService> Logger { get; set; }
    
    
    private HttpClient HttpClient { get; set; }
    
    private DashboardSettings DashboardSettings { get; set; }
    
    public BrowserService(
        ILogger<BrowserService> logger,
        HttpClient httpClient,
        IOptions<DashboardSettings> dashboardSettings)
    {
        this.Logger = logger;
        this.HttpClient = httpClient;
        this.DashboardSettings = dashboardSettings.Value;
    }
    
    public async Task<bool> IsExpectedUrlOpenAsync(string expectedUrl)
    {
        try
        {
            var tabs = await HttpClient.GetFromJsonAsync<List<ChromiumTab>>(new Uri(this.DashboardSettings.BaseUrl, this.DashboardSettings.DashboardPath));
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