using System.Diagnostics;

namespace AlarmService.Services;

public class CecService
{
    /// <summary>
    /// Gets a value indicating whether the monitor is currently turned on.
    /// </summary>
    public bool IsMonitorOn { get; private set; }
    
    /// <summary>
    /// Gets or sets the logger for the <see cref="CecService"/>.
    /// </summary>
    private ILogger<CecService> Logger { get; set; }

    public CecService(ILogger<CecService> logger)
    {
        Logger = logger;
    }
    /// <summary>
    /// Sends the CEC command to turn on the device.
    /// </summary>
    public void TurnOn()
    {
        this.Logger.LogInformation("Turning on the device via CEC.");
        // Implement logic to turn on the device using CEC
        Console.WriteLine("Turning on the device via CEC.");
        
        this.ExecuteCecCommand("on 0");
        this.IsMonitorOn = true;
    }
    
    /// <summary>
    /// Sends the CEC command to turn off the device.
    /// </summary>
    public void TurnOff()
    {
        this.Logger.LogInformation("Turning off the device via CEC.");
        // Implement logic to turn off the device using CEC
        Console.WriteLine("Turning off the device via CEC.");
        
        this.ExecuteCecCommand("standby 0");
        this.IsMonitorOn = false;
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