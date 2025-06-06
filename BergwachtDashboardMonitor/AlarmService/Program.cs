using AlarmService.Services;
using AlarmService.Settings;

namespace AlarmService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        
        ConfigureAppSettings(builder);
        
        ConfigureLogging(builder.Services);
        ConfigureHttpClient(builder.Services);
        
        ConfigureServices(builder.Services);

        var host = builder.Build();
        host.Run();
    }
    
    /// <summary>
    /// Configures the application settings.
    /// </summary>
    /// <param name="builder">The WebAssembly host builder.</param>
    private static void ConfigureAppSettings(HostApplicationBuilder builder)
    {
        builder.Services.Configure<CompanionSettings>(builder.Configuration.GetSection("ServerOptions"));
        builder.Services.Configure<AlarmSettings>(builder.Configuration.GetSection("AlarmSettings"));
        builder.Services.Configure<FilterSettings>(builder.Configuration.GetSection("Filter"));
    }

    private static void ConfigureLogging(IServiceCollection services)
    {
        services.AddLogging(configure => configure.AddConsole());
    }

    private static void ConfigureHttpClient(IServiceCollection services)
    {
        services.AddHttpClient<HttpRequestService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AlarmService/1.0");
        });
    }
    
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<AlarmFilterService>();
        services.AddSingleton<CecService>();
        services.AddSingleton<CompanionService>();
        services.AddSingleton<GraphQlQueryService>();
        services.AddHostedService<WorkerService>();
    }
}