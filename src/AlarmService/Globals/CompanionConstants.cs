namespace AlarmService.Globals;

public static class CompanionConstants
{
    private static string AuthenticationUrl { get; set; }= "https://passport.services.bergwacht-bayern.org/oauth/import?token=";
    
    public static string DashboardUrl { get; set; } = "https://pages.services.bergwacht-bayern.org/dashboard";
    
    public static string GetFullDashboardUrl(string? authToken, string? dashboardState)
    {
        if (string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(dashboardState))
        {
            throw new ArgumentException("Auth token and dashboard state must not be null or empty.");
        }
        
        var fullUrl = $"{AuthenticationUrl}{authToken}&redirect={DashboardUrl}?dashboardState={dashboardState}";
        return fullUrl;
    }
}