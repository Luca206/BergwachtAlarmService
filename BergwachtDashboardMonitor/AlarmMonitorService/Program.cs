using AlarmMonitorService.Services;

namespace AlarmMonitorService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Lese vollständige URL aus Konfiguration
        var baseUrl = builder.Configuration.GetValue<string>("ServerOptions:BaseUrl");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new Exception("ServerOptions:BaseUrl ist nicht definiert.");
        }
        
        var token = builder.Configuration.GetValue<string>("ServerOptions:Token");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new Exception("ServerOptions:Token ist nicht definiert.");
        }
        
        var url = $"{baseUrl}?token={token}";
        builder.WebHost.UseUrls(url);
        
        // Logging konfigurieren
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        builder.Services.AddSingleton<CecController>();
        builder.Services.AddSingleton<WebSocketHandler>();
        builder.Services.AddHostedService<AlarmService>();

        var app = builder.Build();

        app.UseWebSockets();
        app.Use(async (context, next) =>
        {
            if (context.Request.Path == "/ws")
            {
                var handler = app.Services.GetRequiredService<WebSocketHandler>();
                await handler.HandleAsync(context);
            }
            else
            {
                await next();
            }
        });

        app.Run();
    }
}