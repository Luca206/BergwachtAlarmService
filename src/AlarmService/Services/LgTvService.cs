using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using AlarmService.Globals;

namespace AlarmService.Services;

using System.Net;
using System.Net.Sockets;
using global::AlarmService.Services.Interfaces;
using global::AlarmService.Settings;
using Microsoft.Extensions.Options;

public class LgTvService : ITvService
{
  private readonly ILogger<LgTvService> _logger;

  private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
  };

  private ClientWebSocket? _ws;
  private Task? _receiveLoopTask;
  private CancellationTokenSource? _cts;
  private readonly SemaphoreSlim _sendLock = new(1, 1);

  // map of message id -> completion source with raw json response
  private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _pending = new();
  
  private TvSettings TvSettings { get; }

  public LgTvService(
    ILogger<LgTvService> logger,
    IOptions<TvSettings> tvSettings)
  {
    _logger = logger;
    this.TvSettings = tvSettings.Value;
  }

  /// <inheritdoc />
  public async Task ConnectAsync(string ip, CancellationToken cancellationToken = default)
  {
    if (_ws is { State: WebSocketState.Open }) return;

    await DisposeInternalAsync().ConfigureAwait(false);

    _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    _ws = new ClientWebSocket();
    var uri = new Uri($"ws://{ip}:3000/");
    _logger.LogInformation("Connecting to LG webOS TV at {Uri}", uri);
    await _ws.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);

    // start receive loop
    _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));

    // send register
    var registerMsg = new
    {
      type = "register",
      payload = new
      {
        manifest = new
        {
          manifestVersion = 1,
          appVersion = "1.0",
          signed = new
          {
            created = "20220101",
            appId = "com.lg.bwa.alarmservice",
            vendorId = "com.bergwacht",
            localizedAppNames = new Dictionary<string, string> { [""] = "Bergwacht AlarmService" },
            permissions = new[] { "LAUNCH", "CONTROL", "INPUT_SOCKET", "INPUT_KEY" },
            serial = "1"
          },
          permissions = new[] { "LAUNCH", "CONTROL", "INPUT_SOCKET", "INPUT_KEY" }
        }
      }
    };

    await SendRawAsync(JsonSerializer.SerializeToUtf8Bytes(registerMsg, _jsonOptions), cancellationToken).ConfigureAwait(false);
    _logger.LogInformation("Registration message sent to TV.");
  }

  /// <summary>
  /// Displays a toast message on the TV.
  /// </summary>
  /// <param name="message">Message text to display.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  public Task ShowToastAsync(string message, CancellationToken cancellationToken = default)
    => SendRequestAsync<JsonElement>(LgWebOsCommands.CreateToast, new { message }, cancellationToken);

  /// <inheritdoc />
  public async Task<bool> IsScreenOnAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      var response = await SendRequestAsync<JsonElement>(
        LgWebOsCommands.GetPowerState,
        null,
        cancellationToken).ConfigureAwait(false);

      if (response.ValueKind == JsonValueKind.Undefined || response.ValueKind == JsonValueKind.Null)
      {
        return true;// assume on when connected and response missing
      }

      if (response.TryGetProperty("state", out var stateProp))
      {
        var state = stateProp.GetString() ?? string.Empty;
        return state.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
               state.Equals("On", StringComparison.OrdinalIgnoreCase) ||
               state.Equals("Screen On", StringComparison.OrdinalIgnoreCase);
      }

      // Some firmwares return nested payload
      if (response.TryGetProperty("payload", out var payload) && payload.TryGetProperty("state", out var nested))
      {
        var state = nested.GetString() ?? string.Empty;
        return state.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
               state.Equals("On", StringComparison.OrdinalIgnoreCase) ||
               state.Equals("Screen On", StringComparison.OrdinalIgnoreCase);
      }

      return true;
    }
    catch
    {
      // If the socket is up, most likely the screen is on; otherwise ConnectAsync would fail.
      return true;
    }
  }

  /// <inheritdoc />
  public Task TurnOnAsync(CancellationToken cancellationToken = default)
  {
    WakeOnLan(this.TvSettings.MacAddress);
    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public async Task TurnOffAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      // Preferred endpoint
      await SendRequestAsync<JsonElement>(
        LgWebOsCommands.TvPowerTurnOff,
        null,
        cancellationToken).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      _logger.LogDebug(ex, "turnOff via tvpower service failed, falling back to system/turnOff.");
      await SendRequestAsync<JsonElement>(
        LgWebOsCommands.SystemTurnOff,
        null,
        cancellationToken).ConfigureAwait(false);
    }
  }

  /// <summary>
  /// Sends a request to the TV API and awaits the corresponding response.
  /// </summary>
  /// <param name="uri">webOS API uri (e.g. "ssap://system.notifications/createToast").</param>
  /// <param name="payload">Optional payload object that will be serialized to JSON.</param>
  /// <param name="cancellationToken">Cancellation token to cancel the request.</param>
  /// <typeparam name="TResponse">Type of the response payload to deserialize into.</typeparam>
  /// <returns>Deserialized response payload if available; otherwise default.</returns>
  public async Task<TResponse?> SendRequestAsync<TResponse>(string uri, object? payload = null, CancellationToken cancellationToken = default)
  {
    this.EnsureConnected();
    var id = Guid.NewGuid().ToString();

    var msg = new
    {
      id,
      type = "request",
      uri,
      payload,
    };

    var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
    _pending[id] = tcs;

    await SendRawAsync(JsonSerializer.SerializeToUtf8Bytes(msg, _jsonOptions), cancellationToken).ConfigureAwait(false);

    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    linkedCts.CancelAfter(TimeSpan.FromSeconds(10));

    try
    {
      using (linkedCts.Token.Register(() => tcs.TrySetCanceled(linkedCts.Token)))
      {
        var json = await tcs.Task.ConfigureAwait(false);
        if (typeof(TResponse) == typeof(JsonElement))
        {
          return (TResponse?)(object)json;
        }
        if (json.TryGetProperty("payload", out var payloadElement))
        {
          return payloadElement.Deserialize<TResponse>(_jsonOptions);
        }
        return json.Deserialize<TResponse>(_jsonOptions);
      }
    }
    finally
    {
      _pending.TryRemove(id, out _);
    }
  }

  private async Task SendRawAsync(byte[] data, CancellationToken cancellationToken)
  {
    this.EnsureConnected();
    await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
      await _ws!.SendAsync(data, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
    }
    finally
    {
      _sendLock.Release();
    }
  }

  private void EnsureConnected()
  {
    if (_ws is null || _ws.State != WebSocketState.Open)
    {
      throw new InvalidOperationException("LG webOS service is not connected. Call ConnectAsync first.");
    }
  }

  private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
  {
    var buffer = new byte[32 * 1024];
    var sb = new StringBuilder();
    var encoding = Encoding.UTF8;
    try
    {
      while (!cancellationToken.IsCancellationRequested && _ws is { State: WebSocketState.Open })
      {
        var result = await _ws.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
        if (result.MessageType == WebSocketMessageType.Close)
        {
          _logger.LogWarning("WebSocket closed by TV: {CloseStatus} {Description}", _ws.CloseStatus, _ws.CloseStatusDescription);
          break;
        }

        sb.Clear();
        sb.Append(encoding.GetString(buffer, 0, result.Count));
        while (!result.EndOfMessage)
        {
          result = await _ws.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
          sb.Append(encoding.GetString(buffer, 0, result.Count));
        }

        var text = sb.ToString();
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement.Clone();

        if (root.TryGetProperty("id", out var idProp))
        {
          var id = idProp.GetString() ?? string.Empty;
          if (_pending.TryGetValue(id, out var tcs))
          {
            tcs.TrySetResult(root);
          }
        }
        else
        {
          _logger.LogDebug("Received message without id: {Message}", text);
        }
      }
    }
    catch (OperationCanceledException)
    {
      // normal during shutdown
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error in LG webOS receive loop.");
    }
  }

  private async ValueTask DisposeInternalAsync()
  {
    try
    {
      await _cts?.CancelAsync();
    }
    catch
    {
      /* ignore */
    }

    if (_receiveLoopTask is not null)
    {
      try { await _receiveLoopTask.ConfigureAwait(false); }
      catch
      {
        /* ignore */
      }
    }

    if (_ws is not null)
    {
      try
      {
        if (_ws.State == WebSocketState.Open)
        {
          await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disposing", CancellationToken.None).ConfigureAwait(false);
        }
      }
      catch
      {
        /* ignore */
      }
      finally
      {
        _ws.Dispose();
      }
    }

    _ws = null;
    _receiveLoopTask = null;
    _cts?.Dispose();
    _cts = null;

    // cancel any pending requests
    foreach (var kvp in _pending)
    {
      kvp.Value.TrySetCanceled();
    }
    _pending.Clear();
  }
  
  private static void WakeOnLan(string macAddress)
  {
    // MAC-String in Byte-Array umwandeln
    byte[] macBytes = macAddress
      .Split(':', '-', '.')
      .Select(b => Convert.ToByte(b, 16))
      .ToArray();

    if (macBytes.Length != 6)
      throw new ArgumentException("Ungültige MAC-Adresse.");

    // Magic Packet aufbauen: 6x 0xFF + 16x MAC
    byte[] packet = new byte[6 + 16 * macBytes.Length];
    for (int i = 0; i < 6; i++)
      packet[i] = 0xFF;
    for (int i = 6; i < packet.Length; i += macBytes.Length)
      Buffer.BlockCopy(macBytes, 0, packet, i, macBytes.Length);

    // UDP-Client für Broadcast
    using (UdpClient client = new UdpClient())
    {
      client.EnableBroadcast = true;
      client.Connect(IPAddress.Broadcast, 9); // Port 9 = "discard"
      client.Send(packet, packet.Length);
    }
  }
}