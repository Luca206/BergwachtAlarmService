using System.Diagnostics;

namespace AlarmService.Services;

public class CecService
{
    private ILogger<CecService> Logger { get; set; }

    public CecService(ILogger<CecService> logger)
    {
        Logger = logger;
    }
    
    public void TuronOn()
    {
        // Implement logic to turn on the device using CEC
        Console.WriteLine("Turning on the device via CEC.");
    }
    
    public void TurnOff()
    {
        // Implement logic to turn off the device using CEC
        Console.WriteLine("Turning off the device via CEC.");
    }
    
    private void ExecuteCecCommand(string command)
    {
        try
        {
            Process.Start("bash", $"-c \"echo '{command}' | cec-client -s -d 1\"");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Fehler beim Ausführen des CEC-Kommandos: {Command}", command);
        }
    }
}