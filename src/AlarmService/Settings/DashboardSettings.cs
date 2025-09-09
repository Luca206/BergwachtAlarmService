namespace AlarmService.Settings;

public class DashboardSettings
{
    public Uri AuthenticationUrl { get; set; }
    
    public Uri BaseUrl { get; set; }

    public string DashboardPath { get; set; }

    public string AccessToken { get; set; }

    public string DashboardToken { get; set; }
}