using AlarmService.Services;
using AlarmService.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AlarmServiceTests.Services.AlarmFilterServiceTests;

public class FilterSettingsUpdatedTests
{
    private AlarmFilterService _service;
    private Mock<ILogger<AlarmFilterService>> _loggerMock;
    private FilterSettings _filterSettings;
    private AlarmSettings _alarmSettings;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<AlarmFilterService>>();
        _filterSettings = new FilterSettings
        {
            FilterEntries = new List<FilterEntry>
            {
                new FilterEntry { PropertyPath = "id", Value = "1", IsInclude = true }
            }
        };
        _alarmSettings = new AlarmSettings { KeepMonitorTurnedOnInSec = 60 };
        _service = new AlarmFilterService(
            _loggerMock.Object,
            Options.Create(_filterSettings),
            Options.Create(_alarmSettings));
    }

    [Test]
    public void FilterSettingsUpdated_ReturnsTrue_WhenSettingsChanged()
    {
        var newSettings = new FilterSettings
        {
            FilterEntries = new List<FilterEntry>
            {
                new FilterEntry { PropertyPath = "id", Value = "2", IsInclude = true }
            }
        };
        var result = _service.FilterSettingsUpdated(newSettings);
        Assert.That(result, Is.True);
    }

    [Test]
    public void FilterSettingsUpdated_ReturnsFalse_WhenSettingsSame()
    {
        var newSettings = new FilterSettings
        {
            FilterEntries = new List<FilterEntry>
            {
                new FilterEntry { PropertyPath = "id", Value = "1", IsInclude = true }
            }
        };
        var result = _service.FilterSettingsUpdated(newSettings);
        Assert.That(result, Is.False);
    }
}