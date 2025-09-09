using Bwb.GraphQL.Client;

namespace AlarmService.Dtos;

public class GetAlarmsResponse
{
    public required AlarmsDto Alarms { get; set; }
}

public class AlarmsDto
{
    public bool HasNextPage { get; set; }
    
    public required IReadOnlyCollection<Alarm> Results { get; set; }
}