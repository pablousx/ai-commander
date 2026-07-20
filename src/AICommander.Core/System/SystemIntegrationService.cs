using Microsoft.Win32;

namespace AICommander.Core.System;

public class SystemIntegrationService
{
    private const string RegistryRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "AICommander";

    /// <summary>
    /// Sets or removes the application from the Windows startup registry.
    /// </summary>
    /// <param name="enable">True to add to startup, false to remove.</param>
    public void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, true);
            if (key == null) return;

            if (enable)
            {
                string? exePath = global::System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
            }
            else
            {
                if (key.GetValue(AppName) != null)
                {
                    key.DeleteValue(AppName, false);
                }
            }
        }
        catch (Exception ex)
        {
            // Ideally we would log this
            Console.WriteLine($"Failed to set auto-start: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if the application is currently registered for auto-start.
    /// </summary>
    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }
}
