using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Push notification + Notification Hubs namespaces
using Windows.Networking.PushNotifications;
using Microsoft.WindowsAzure.Messaging;
using Windows.UI.Popups;

namespace UwpClientApp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        // ─────────────────────────────────────────────────────────────────────────
        //  TODO: set these two values for YOUR notification hub.
        //
        //  HubName                   → the notification hub name (e.g. "wns-demo-hub").
        //  HubListenConnectionString → the DefaultListenSharedAccessSignature
        //                              connection string from the hub's Access Policies.
        //
        //  Use the LISTEN (not Full) connection string in the client app.
        // ─────────────────────────────────────────────────────────────────────────
        private const string HubName = "wns-demo-hub";
        private const string HubListenConnectionString =
            "Endpoint=sb://wns-demo-ns-st.servicebus.windows.net/;SharedAccessKeyName=DefaultListenSharedAccessSignature;SharedAccessKey=pT2hiGcE6+1UUhJ8YbtFEooy4HCoTb8dV/myCe5g9j0=";

        /// <summary>
        /// Initializes the singleton application object.
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Requests a WNS channel for this device and registers it with the
        /// Azure Notification Hub so the device can receive push notifications.
        /// </summary>
        private async void InitNotificationsAsync()
        {
            // 1. Ask WNS for a push channel (the per-device "mailbox" URI).
            var channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();

            // 2. Register that channel URI with the notification hub.
            var hub = new NotificationHub(HubName, HubListenConnectionString);
            var result = await hub.RegisterNativeAsync(channel.Uri);

            // 3. Show the registration id so you know it worked.
            if (result.RegistrationId != null)
            {
                var dialog = new MessageDialog("Registration successful: " + result.RegistrationId);
                dialog.Commands.Add(new UICommand("OK"));
                await dialog.ShowAsync();
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.
        /// </summary>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            // Register for push notifications on every launch so the channel URI
            // (which can change) is always current in the hub.
            InitNotificationsAsync();

            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Load state from previously suspended application.
                }

                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }

                Window.Current.Activate();
            }
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            // TODO: Save application state and stop any background activity.
            deferral.Complete();
        }
    }
}
