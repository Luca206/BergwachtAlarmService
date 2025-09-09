namespace AlarmService.Settings
{
  /// <summary>
  /// Settings for the target TV to control via LG webOS service.
  /// </summary>
  public class TvSettings
  {
    /// <summary>
    /// IPv4/IPv6 address or hostname of the TV running webOS.
    /// </summary>
    public string IpAddress { get; set; }
    
    /// <summary>
    /// Optional MAC address of the TV. Reserved for future Wake-on-LAN scenarios.
    /// </summary>
    public string MacAddress { get; set; }
  }
}