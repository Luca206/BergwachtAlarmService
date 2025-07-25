using System.Diagnostics;

namespace AlarmService.Services;

public class CecService
{
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
        this.ExecuteCecCommand("on 0");
        
    }

    /// <summary>
    /// Sends the CEC command to turn off the device.
    /// </summary>
    public void TurnOff()
    {
        this.Logger.LogInformation("Turning off the device via CEC.");
        this.ExecuteCecCommand("standby 0");
    }

    protected virtual void ExecuteCecCommand(string command)
    {
        try
        {
            Process.Start("bash", $"-c \"echo '{command}' | cec-client -s -d 1\"");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error while executing the CEC-Command: {Command}", command);
        }
    }

    public async Task<bool> IsMonitorActive()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = "-c \"echo 'pow 0' | cec-client -s -d 1\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                Logger.LogError("Error starting cec-client process.");
                return false;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (output.Contains("power status: on", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            if (output.Contains("power status: standby", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            throw new Exception("Could not determine power status from CEC output.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error while checking monitor status via CEC.");
            return false;
        }
    }
}