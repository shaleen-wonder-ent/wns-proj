using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UwpClientApp
{
    /// <summary>
    /// The application's main window. It hosts a <see cref="MainPage"/> in a navigation frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            // Show the main page on both Windows 10 and Windows 11.
            PageFrame.Navigate(typeof(MainPage));
        }
    }
}
