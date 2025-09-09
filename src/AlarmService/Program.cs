using AlarmService.Services;
using AlarmService.Services.Interfaces;
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

        // 1) Bind simple path holder settings from appsettings.*
        builder.Services.Configure<FilterSettings>(builder.Configuration.GetSection("FilterSettings"));
        builder.Services.Configure<ConfigSettings>(builder.Configuration.GetSection("Config"));

        // 2) Resolve external files (absolute or relative) and add their configuration or map to options
        var filterPath = builder.Configuration.GetSection("FilterSettings:FilterFilePath").Value;
        var configPath = builder.Configuration.GetSection("Config:ConfigFilePath").Value;

        filterPath = ResolvePath(env.ContentRootPath, filterPath);
        configPath = ResolvePath(env.ContentRootPath, configPath);

        // Load external config.json into configuration so sections are available for DI binding
        if (!string.IsNullOrWhiteSpace(configPath) && File.Exists(configPath))
        {
            builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: true);
        }
        else if (!string.IsNullOrWhiteSpace(configPath))
        {
            Log.Warning("Config file not found at path: {Path}", configPath);
        }

        // Configure options from external config.json sections
        builder.Services.Configure<GraphQlSettings>(builder.Configuration.GetSection("GraphQl"));
        builder.Services.Configure<TvSettings>(builder.Configuration.GetSection("TvConfig"));
        builder.Services.Configure<DashboardSettings>(builder.Configuration.GetSection("Dashboard"));
        builder.Services.Configure<WorkerSettings>(builder.Configuration.GetSection("WorkerConfig"));
        
        // Load external filter.json into configuration so sections are available for DI binding
        if (!string.IsNullOrWhiteSpace(filterPath) && File.Exists(filterPath))
        {
            builder.Configuration.AddJsonFile(filterPath, optional: false, reloadOnChange: true);
        }
        else if (!string.IsNullOrWhiteSpace(filterPath))
        {
            Log.Warning("Filter file not found at path: {Path}", filterPath);
        }
        
        // Configure options from external config.json sections
        builder.Services.Configure<FilterSettings>(builder.Configuration.GetSection("Filter:Entries"));
    }

    private static string ResolvePath(string contentRoot, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;

        // Expand environment variables
        var expanded = Environment.ExpandEnvironmentVariables(path);

        // Expand tilde for home
        if (expanded.StartsWith("~"))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            expanded = Path.Combine(home, expanded.TrimStart('~', '/', '\\'));
        }

        // If relative, make absolute relative to content root
        if (!Path.IsPathRooted(expanded))
        {
            expanded = Path.GetFullPath(Path.Combine(contentRoot, expanded));
        }

        return expanded;
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
        services.AddSingleton<Services.AlarmService>();
        services.AddSingleton<AlarmFilterService>();
        services.AddSingleton<CompanionService>();
        services.AddSingleton<GraphQlQueryService>();
        services.AddSingleton<BrowserService>();
        services.AddSingleton<ITvService, LgTvService>();
        services.AddHostedService<WorkerService>();
    }
}