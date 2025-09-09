using AlarmService.Settings;
using Microsoft.Extensions.Options;

namespace AlarmService.Services;

public class HttpRequestService
{
    private ILogger<HttpRequestService> Logger { get; set; }
    
    private HttpClient HttpClient { get; set; }
    
    private GraphQlSettings GraphQlSettings { get; set; }
    
    public HttpRequestService(
        HttpClient httpClient,
        ILogger<HttpRequestService> logger,
        IOptions<GraphQlSettings> settings)
    {
        this.Logger = logger;
        this.HttpClient = httpClient;
        this.GraphQlSettings = settings.Value;
    }
    
    public async Task<HttpResponseMessage> GetAsync(
        string endpoint,
        Dictionary<string, string>? parameters = null)
    {
        return await this.SendRequestAsync(HttpMethod.Get, endpoint, parameters).ConfigureAwait(false);
    }

    public virtual async Task<HttpResponseMessage> PostAsync(
        string endpoint,
        Dictionary<string, string>? parameters = null,
        HttpContent? content = null)
    {
        return await this.SendRequestAsync(HttpMethod.Post, endpoint, parameters, content).ConfigureAwait(false);
    }
    
    public virtual Uri BuildCompleteRequestUrl(Uri baseUrl, string endpoint, Dictionary<string, string>? parameters)
    {
        if (baseUrl == null)
        {
            throw new ArgumentException("The request uri cannot be null.");
        }

        var builder = new UriBuilder(new Uri(baseUrl, endpoint));
        if (parameters != null && parameters.Any())
        {
            builder.Query = string.Join(
                "&",
                parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        }

        return builder.Uri;
    }

    public async Task<HttpResponseMessage> SendRequestAsync(
        HttpMethod method,
        string endpoint,
        Dictionary<string, string>? parameters,
        HttpContent? content = null)
    {
        var completeRequestUrl = this.BuildCompleteRequestUrl(this.GraphQlSettings.BaseUrl, endpoint, parameters);
        using var request = new HttpRequestMessage(method, completeRequestUrl);
        request.Headers.Add("Authorization", $"Bearer {this.GraphQlSettings.AccessToken}");
        request.Content = content;
        var response = await this.HttpClient.SendAsync(request).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            this.Logger.LogWarning($"Request to {request.RequestUri} failed with status code {response.StatusCode}.");
        }

        return response;
    }
}