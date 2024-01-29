using Microsoft.Win32;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Browser_Selector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            string url = ((App)Application.Current).StartupUrl;

            // Use the URL as needed
            if (!string.IsNullOrEmpty(url))
            {
                // Process the URL in your MainWindow
                // For example, update a control with the URL
                startupURl.Text = url;
            }
            //RegisterProtocolHandler("http");
            //RegisterProtocolHandler("https");
        }

        private void RegisterProtocolHandler(string protocol)
        {
            try
            {
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(protocol))
                {
                    if (key != null)
                    {
                        key.SetValue("", "URL:" + protocol + " Protocol");
                        key.SetValue("URL Protocol", "");

                        using (RegistryKey defaultIcon = key.CreateSubKey("DefaultIcon"))
                        {
                            if (defaultIcon != null)
                                defaultIcon.SetValue("", "path-to-your-app-icon.ico");
                        }

                        using (RegistryKey commandKey = key.CreateSubKey(@"shell\open\command"))
                        {
                            if (commandKey != null)
                                commandKey.SetValue("", "path-to-your-app.exe \"%1\"");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception
                MessageBox.Show("An error occurred while registering the protocol handler: " + ex.Message);
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(startupURl.Text, TextDataFormat.Text);
        }

        private void OpenUrlWithBrowser(string url, string prowserPath)
        {
            try
            {
                // Use the default browser (in this case, Microsoft Edge) to open the URL
                Process.Start(new ProcessStartInfo
                {
                    FileName = prowserPath,
                    Arguments = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                // Handle exceptions, e.g., if Microsoft Edge is not installed
                MessageBox.Show("An error occurred: " + ex.Message);
            }
        }

        private void EdgeButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUrlWithBrowser(startupURl.Text, "msedge.exe");
        }
    }
}