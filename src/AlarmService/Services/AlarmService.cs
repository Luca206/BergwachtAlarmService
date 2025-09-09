namespace AlarmService.Services
{
  using global::AlarmService.Dtos;

  public class AlarmService
  {
    private CompanionService CompanionService { get; }
    
    public AlarmService(CompanionService companionService)
    {
      this.CompanionService = companionService;
    }
    
    public async Task<IReadOnlyCollection<DetectedAlarm>> GetAlarms()
    {
      var alarms = await this.CompanionService.CheckForAlarms();
      var activeAlarms = GetActiveAlarms(alarms);
      return activeAlarms;
    }

    private IReadOnlyCollection<DetectedAlarm> GetActiveAlarms(IReadOnlyCollection<DetectedAlarm>? alarms)
    {
      if (alarms == null)
      {
        return Array.Empty<DetectedAlarm>();
      }
      
      var result = alarms.Where(a => a.ExpirationTime > DateTime.Now).ToList().AsReadOnly();
      return result;
    }
  }
}