namespace AlarmService.Services.Interfaces
{
  /// <summary>
  /// Interface for the TV-Service.
  /// </summary>
  public interface ITvService
  {
    /// <summary>
    /// Connects to the TV at the given IP address and performs the webOS registration handshake.
    /// </summary>
    /// <param name="ip">IPv4/IPv6 address or hostname of the LG TV.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the connection attempt.</param>
    Task ConnectAsync(string ip, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if the TV is on. 
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the TV is on.</returns>
    Task<bool> IsScreenOnAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Turns the TV on.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing an asynchronous operation.</returns>
    Task TurnOnAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Turns the TV off.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing an asynchronous operation.</returns>
    Task TurnOffAsync(CancellationToken cancellationToken = default);
  }
}