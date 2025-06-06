using System.Net.WebSockets;
using System.Text;

namespace AlarmMonitorService.Services;

public class WebSocketHandler
{
    private readonly CecController _cecController;
    private readonly ILogger<WebSocketHandler> _logger;

    public WebSocketHandler(CecController cecController, ILogger<WebSocketHandler> logger)
    {
        _cecController = cecController;
        _logger = logger;
    }

    public async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        _logger.LogInformation("WebSocket-Verbindung hergestellt.");

        var buffer = new byte[1024 * 4];
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

            _logger.LogInformation("Nachricht empfangen: {Message}", message);

            switch (message.ToLowerInvariant())
            {
                case "alarm":
                    _cecController.TurnOn();
                    break;
                case "reset":
                    _cecController.TurnOff();
                    break;
                default:
                    _logger.LogWarning("Unbekannte Nachricht: {Message}", message);
                    break;
            }
        }

        _logger.LogInformation("WebSocket-Verbindung geschlossen.");
    }
}