using System.Configuration;
using System.Data;
using System.Windows;

namespace Browser_Selector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private string startupUrl;

        public string StartupUrl
        {
            get { return startupUrl; }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // base.OnStartup(e);

            // Check for command line arguments
            if (e.Args.Length > 0 && (e.Args[0].StartsWith("https:") || e.Args[0].StartsWith("http:")))
            {
                startupUrl = e.Args[0];
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
            }

        }
    }

}
