using System.Diagnostics;

namespace AlarmMonitorService.Services;

public class CecController
{
    private readonly ILogger<CecController> _logger;

    public CecController(ILogger<CecController> logger)
    {
        _logger = logger;
    }

    public void TurnOn()
    {
        _logger.LogInformation("TV wird per CEC eingeschaltet.");
        ExecuteCecCommand("on 0");
    }

    public void TurnOff()
    {
        _logger.LogInformation("TV wird per CEC ausgeschaltet.");
        ExecuteCecCommand("standby 0");
    }

    private void ExecuteCecCommand(string command)
    {
        try
        {
            Process.Start("bash", $"-c \"echo '{command}' | cec-client -s -d 1\"");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Ausführen des CEC-Kommandos: {Command}", command);
        }
    }
}