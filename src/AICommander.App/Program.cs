using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace AICommander.App;

public class Program
{
    private static Mutex? _mutex;

    [STAThread]
    public static void Main()
    {
        const string mutexName = "Global\\AICommanderSingleInstance";
        _mutex = new Mutex(true, mutexName, out bool isNewInstance);

        if (!isNewInstance)
        {
            MessageBox.Show("AI Commander is already running.", "AI Commander", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var app = new AICommander.App.App();
            app.InitializeComponent();
            app.Run();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Crash in Main: {ex}", "AI Commander Crash", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
    }
}
