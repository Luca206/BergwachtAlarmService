using System.Net;
using System.Text.Json;
using AlarmService.Dtos;
using AlarmService.Services;
using AlarmService.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AlarmServiceTests.Services.CompanionServiceTests;

[TestFixture]
public class ServiceTests
{
    private Mock<ILogger<CompanionService>> _loggerMock;
    private Mock<GraphQlQueryService> _queryServiceMock;
    private Mock<HttpRequestService> _httpRequestServiceMock;
    private Mock<AlarmFilterService> _alarmFilterServiceMock;
    private CompanionService _service;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<CompanionService>>();

        _queryServiceMock = new Mock<GraphQlQueryService>(
            new Mock<ILogger<GraphQlQueryService>>().Object,
            new Mock<IOptions<AlarmSettings>>().Object);

        _httpRequestServiceMock = new Mock<HttpRequestService>(
            new Mock<HttpClient>().Object,
            new Mock<ILogger<HttpRequestService>>().Object,
            new Mock<IOptions<CompanionSettings>>().Object);

        _alarmFilterServiceMock = new Mock<AlarmFilterService>(
            new Mock<ILogger<AlarmFilterService>>().Object,
            new Mock<IOptions<FilterSettings>>().Object,
            new Mock<IOptions<AlarmSettings>>().Object);

        _service = new CompanionService(
            _loggerMock.Object,
            _queryServiceMock.Object,
            new Mock<IOptions<FilterSettings>>().Object,
            new Mock<IOptions<AlarmSettings>>().Object);
    }

    [Test]
    public async Task CheckForAlarms_ReturnsNull_WhenResponseIsNullOrEmpty()
    {
        _queryServiceMock.Setup(q => q.BuildQuery()).Returns("query");
        _httpRequestServiceMock.Setup(h => h.PostAsync(It.IsAny<string>(), null, It.IsAny<HttpContent>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") });

        var result = await _service.CheckForAlarms();

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CheckForAlarms_ReturnsNull_WhenJsonIsInvalid()
    {
        _queryServiceMock.Setup(q => q.BuildQuery()).Returns("query");
        _httpRequestServiceMock.Setup(h => h.PostAsync(It.IsAny<string>(), null, It.IsAny<HttpContent>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent("{\"invalid\":true}") });

        var result = await _service.CheckForAlarms();

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CheckForAlarms_ReturnsNull_WhenResultsArrayIsEmpty()
    {
        var json = "{\"data\":{\"alarms\":{\"results\":[]}}}";
        _queryServiceMock.Setup(q => q.BuildQuery()).Returns("query");
        _httpRequestServiceMock.Setup(h => h.PostAsync(It.IsAny<string>(), null, It.IsAny<HttpContent>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });

        var result = await _service.CheckForAlarms();

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CheckForAlarms_ReturnsNull_WhenNoAlarmsMatchFilter()
    {
        var json = "{\"data\":{\"alarms\":{\"results\":[{\"id\":1}]}}}";
        _queryServiceMock.Setup(q => q.BuildQuery()).Returns("query");
        _httpRequestServiceMock.Setup(h => h.PostAsync(It.IsAny<string>(), null, It.IsAny<HttpContent>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        _alarmFilterServiceMock.Setup(a => a.FilterAlarmsOfJsonElement(It.IsAny<JsonElement>()))
            .Returns(new List<DetectedAlarm>());

        var result = await _service.CheckForAlarms();

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CheckForAlarms_ReturnsAlarms_WhenAlarmsMatchFilter()
    {
        var json = "{\"data\":{\"alarms\":{\"results\":[{\"id\":1}]}}}";
        var detectedAlarms = new List<DetectedAlarm> { new() { Id = 1 } };
        _queryServiceMock.Setup(q => q.BuildQuery()).Returns("query");
        _httpRequestServiceMock.Setup(h => h.PostAsync(It.IsAny<string>(), null, It.IsAny<HttpContent>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        _alarmFilterServiceMock.Setup(a => a.FilterAlarmsOfJsonElement(It.IsAny<JsonElement>()))
            .Returns(detectedAlarms);

        var result = await _service.CheckForAlarms();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.First().Id.ToString(), Is.EqualTo("1"));
    }

    [Test]
    public async Task RequestGraphQlAsync_ReturnsNull_WhenStatusCodeIsNotSuccess()
    {
        var privateMethod = typeof(CompanionService).GetMethod("RequestGraphQlAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        _httpRequestServiceMock.Setup(h => h.PostAsync(It.IsAny<string>(), null, It.IsAny<HttpContent>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        var task = (Task<string?>)privateMethod.Invoke(_service, new object[] { "query" });
        var result = await task;

        Assert.That(result, Is.Null);
    }

    [Test]
    public void ValidateJson_ReturnsFalse_WhenJsonIsInvalid()
    {
        var privateMethod = typeof(CompanionService).GetMethod("ValidateJson",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var json = JsonDocument.Parse("{\"invalid\":true}");
        var parameters = new object[] { json, null };
        var result = (bool)privateMethod.Invoke(_service, parameters);

        Assert.That(result, Is.False);
    }

    [Test]
    public void ValidateJson_ReturnsFalse_WhenResultsElementIsNull()
    {
        var privateMethod = typeof(CompanionService).GetMethod("ValidateJson",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var json = JsonDocument.Parse("{\"data\":{\"alarms\":{}}}");
        var parameters = new object[] { json, null };
        var result = (bool)privateMethod.Invoke(_service, parameters);

        Assert.That(result, Is.False);
    }

    [Test]
    public void ValidateJson_ReturnsTrue_WhenJsonIsValid()
    {
        var privateMethod = typeof(CompanionService).GetMethod("ValidateJson",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var json = JsonDocument.Parse("{\"data\":{\"alarms\":{\"results\":[]}}}");
        var resultsElement = json.RootElement.GetProperty("data").GetProperty("alarms").GetProperty("results");
        var parameters = new object[] { json, resultsElement };
        var result = (bool)privateMethod.Invoke(_service, parameters);

        Assert.That(result, Is.True);
    }
}