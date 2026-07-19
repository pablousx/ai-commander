using System.ComponentModel;
using System.Windows;

namespace AICommander.App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Hide_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Instead of closing the application, just hide the window
            e.Cancel = true;
            Hide();
        }
    }
}