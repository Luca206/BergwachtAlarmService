using System.Text.Json.Nodes;
using AlarmService.Dtos;
using AlarmService.Services;
using AlarmService.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AlarmServiceTests.Services.AlarmFilterServiceTests;

public class CreateDetectedAlarmsFromJsonNodeTests
{
    private AlarmFilterService _service;
    private Mock<ILogger<AlarmFilterService>> _loggerMock;
    private FilterSettings _filterSettings;
    private AlarmSettings _alarmSettings;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<AlarmFilterService>>();
        _filterSettings = new FilterSettings { FilterEntries = new List<FilterEntry>() };
        _alarmSettings = new AlarmSettings { KeepMonitorTurnedOnInSec = 60 };
        _service = new AlarmFilterService(
            _loggerMock.Object,
            Options.Create(_filterSettings),
            Options.Create(_alarmSettings));
    }

    [Test]
    public void CreateDetectedAlarmFromJsonNode_ReturnsNull_WhenIdMissing()
    {
        var jsonNode = new JsonObject { ["originatedAt"] = DateTime.UtcNow };
        var result = _service.CreateDetectedAlarmFromJsonNode(jsonNode);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void CreateDetectedAlarmFromJsonNode_ReturnsAlarm_WhenValid()
    {
        var jsonNode = new JsonObject { ["id"] = "1", ["originatedAt"] = DateTime.UtcNow };
        var result = _service.CreateDetectedAlarmFromJsonNode(jsonNode);
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<DetectedAlarm>());
        Assert.That(result.Id, Is.EqualTo(1));
    }
}