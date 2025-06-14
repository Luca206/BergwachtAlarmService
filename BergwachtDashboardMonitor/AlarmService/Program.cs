using AlarmService.Services;
using AlarmService.Settings;
using Serilog;

namespace AlarmService;

public class Program
{
    public static void Main(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
        
        var builder = Host.CreateApplicationBuilder(args);
        
        ConfigureAppSettings(builder);
        
        ConfigureLogging(builder);
        ConfigureHttpClient(builder.Services);
        
        ConfigureServices(builder.Services);

        var host = builder.Build();
        
        try
        {
            Log.Information("Starting host in {Env}", environment);
            host.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled exception");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
    
    
    /// <summary>
    /// Configures the application settings.
    /// </summary>
    /// <param name="builder">The WebAssembly host builder.</param>
    private static void ConfigureAppSettings(HostApplicationBuilder builder)
    {
        builder.Services.Configure<CompanionSettings>(builder.Configuration.GetSection("BackendBWBCompanionSettings"));
        builder.Services.Configure<AlarmSettings>(builder.Configuration.GetSection("AlarmSettings"));
        builder.Services.Configure<FilterSettings>(builder.Configuration.GetSection("Filter"));
    }

    private static void ConfigureLogging(HostApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        builder.Logging.ClearProviders(); // Entfernt Default Logging
        builder.Logging.AddSerilog();
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