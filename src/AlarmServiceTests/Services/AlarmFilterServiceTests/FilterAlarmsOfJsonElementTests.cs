using System.Text.Json;
using AlarmService.Dtos;
using AlarmService.Services;
using AlarmService.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AlarmServiceTests.Services.AlarmFilterServiceTests;

public class FilterAlarmsOfJsonElementTests
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
    public void FilterAlarmsOfJsonElement_ReturnsEmpty_WhenNotArray()
    {
        var json = JsonDocument.Parse("{\"id\":1}").RootElement;
        var result = _service.FilterAlarmsOfJsonElement(json);
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<IReadOnlyCollection<DetectedAlarm>>());
        Assert.That(result.Count, Is.EqualTo(0));
    }

    // [Test]
    // public void FilterAlarmsOfJsonElement_ReturnsFilteredAlarms_WhenArray()
    // {
    //     var jsonArray = new JsonArray(
    //         new JsonObject { ["id"] = 1, ["originatedAt"] = DateTime.UtcNow });
    //     var result = _service.FilterAlarmsOfJsonElement(jsonElement);
    //     Assert.IsNotNull(result);
    // }
}