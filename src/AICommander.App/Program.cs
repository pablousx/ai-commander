using System;
using System.IO;
using System.Windows;

namespace AICommander.App
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                File.WriteAllText("Main_started.txt", "Main started\n");
                var app = new AICommander.App.App();
                app.InitializeComponent();
                app.Run();
            }
            catch (Exception ex)
            {
                File.WriteAllText("crash_log.txt", ex.ToString());
                MessageBox.Show($"Crash in Main: {ex.ToString()}", "AI Commander Crash", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
