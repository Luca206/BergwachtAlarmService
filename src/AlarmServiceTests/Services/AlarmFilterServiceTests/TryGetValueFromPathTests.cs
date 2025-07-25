using System.Text.Json.Nodes;
using AlarmService.Services;
using AlarmService.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AlarmServiceTests.Services.AlarmFilterServiceTests;

public class TryGetValueFromPathTests
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
    public void TryGetValueFromPath_ReturnsTrue_WhenPathExists()
    {
        var jsonNode = new JsonObject { ["id"] = "42" };
        var result = _service.TryGetValueFromPath(jsonNode, "id", out var value);
        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo("42"));
    }

    [Test]
    public void TryGetValueFromPath_ReturnsFalse_WhenPathDoesNotExist()
    {
        var jsonNode = new JsonObject { ["id"] = "42" };
        var result = _service.TryGetValueFromPath(jsonNode, "notfound", out var value);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(value, Is.Null);
        }
    }
}