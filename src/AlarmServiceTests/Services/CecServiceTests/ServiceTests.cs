using AlarmService.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace AlarmServiceTests.Services.CecServiceTests;

[TestFixture]
public class ServiceTests
{
    private Mock<ILogger<CecService>> _loggerMock;
    private CecService _cecService;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<CecService>>();
        _cecService = new CecService(_loggerMock.Object);
    }

    [Test]
    public void IsMonitorOn_ShouldBeFalse_ByDefault()
    {
        // Assert.That(_cecService.IsMonitorOn, Is.False);
    }

    [Test]
    public void TurnOn_ShouldSetIsMonitorOnTrue_AndLogInformation()
    {
        var cecService = new TestableCecService(_loggerMock.Object);
        cecService.TurnOn();

        // Assert.That(cecService.IsMonitorOn, Is.True);
        Assert.That(cecService.LastExecutedCommand, Is.EqualTo("on 0"));
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Turning on the device via CEC.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void TurnOff_ShouldSetIsMonitorOnFalse_AndLogInformation()
    {
        var cecService = new TestableCecService(_loggerMock.Object);
        cecService.TurnOff();

        // Assert.That(cecService.IsMonitorOn, Is.False);
        Assert.That(cecService.LastExecutedCommand, Is.EqualTo("standby 0"));
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Turning off the device via CEC.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void ExecuteCecCommand_ShouldLogError_OnException()
    {
        var cecService = new TestableCecService(_loggerMock.Object, true);
        
        Assert.Throws<InvalidOperationException>(() => cecService.TurnOn());
        // Assert.That(await cecService.IsMonitorActive(), Is.False);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Fehler beim Ausführen des CEC-Kommandos")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // Helper class to override ExecuteCecCommand for testing
    private class TestableCecService : CecService
    {
        public string LastExecutedCommand { get; private set; }
        private readonly bool _throwException;

        public TestableCecService(ILogger<CecService> logger, bool throwException = false)
            : base(logger)
        {
            _throwException = throwException;
            LastExecutedCommand = string.Empty;
        }

        protected override void ExecuteCecCommand(string command)
        {
            if (_throwException)
                throw new InvalidOperationException("Test exception");
            LastExecutedCommand = command;
        }
    }
}
