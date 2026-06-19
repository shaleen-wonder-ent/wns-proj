using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UwpClientApp
{
    /// <summary>
    /// A minimal page that simply explains what the app does and triggers push
    /// registration once the page (and its XamlRoot) is ready.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Run registration here (not in App.OnLaunched) so the page's XamlRoot is
            // available for any ContentDialog the registration flow needs to show.
            await NotificationService.InitNotificationsAsync(this, UpdateStatus);
        }

        private void UpdateStatus(string message)
        {
            StatusTextBlock.Text = message;
        }
    }
}
