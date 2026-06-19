using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UwpClientApp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object. This is the first line of authored
        /// code executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Create and activate the main window (replaces UWP Window.Current usage).
            // Push notification registration is started by MainPage once it has loaded, so the
            // page's XamlRoot is available for any ContentDialog shown by the flow.
            Window = new MainWindow();
            Window.Activate();
        }

        /// <summary>
        /// The application's main window.
        /// </summary>
        public static Window? Window { get; private set; }
    }
}
