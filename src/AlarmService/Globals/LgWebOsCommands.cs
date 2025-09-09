namespace AlarmService.Globals
{
  public class LgWebOsCommands
  {
    /// <summary>
    /// Command to turn the TV via TvPower off.
    /// </summary>
    public const string TvPowerTurnOff = "ssap://com.webos.service.tvpower/power/turnOff";
    
    /// <summary>
    /// Command to turn the TV via system off.
    /// </summary>
    public const string SystemTurnOff = "ssap://system/turnOff";

    /// <summary>
    /// Command to get the power state.
    /// </summary>
    public const string GetPowerState = "ssap://com.webos.service.tvpower/power/getPowerState";
    
    /// <summary>
    /// Command to create a toast.
    /// </summary>
    public const string CreateToast = "ssap://system.notifications/createToast";
  }
}