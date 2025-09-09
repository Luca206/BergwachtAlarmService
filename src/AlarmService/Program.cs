using AlarmService.Services;
using AlarmService.Settings;
using Serilog;

namespace AlarmService;

/// <summary>
/// The main program.
/// </summary>
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
        // Load optional local secrets file (not committed) to avoid storing secrets in appsettings
        var env = builder.Environment;
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("secrets.local.json", optional: true, reloadOnChange: true);

        // Load user secrets in Development
        if (env.IsDevelopment())
        {
            configBuilder.AddUserSecrets<Program>(optional: true);
        }

        configBuilder.AddEnvironmentVariables();

        var configuration = configBuilder.Build();

        // Replace builder.Configuration with the new configuration that includes secrets
        builder.Configuration.AddConfiguration(configuration);

        builder.Services.Configure<CompanionSettings>(builder.Configuration.GetSection("BackendBWBCompanionSettings"));
        builder.Services.Configure<AlarmSettings>(builder.Configuration.GetSection("AlarmSettings"));
        builder.Services.Configure<FilterSettings>(builder.Configuration.GetSection("FilterSettings"));
        builder.Services.Configure<TvSettings>(builder.Configuration.GetSection("TvSettings"));
    }

    private static void ConfigureLogging(HostApplicationBuilder builder)
    {
        Directory.CreateDirectory("logs");

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: "logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

        if (builder.Environment.IsDevelopment())
        {
            loggerConfiguration.MinimumLevel.Debug();
        }
        
        Log.Logger = loggerConfiguration.CreateLogger();


        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);
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
        services.AddSingleton<CompanionService>();
        services.AddSingleton<GraphQlQueryService>();
        services.AddSingleton<BrowserService>();
        services.AddSingleton<LgTvService>();
        services.AddHostedService<WorkerService>();
    }
}