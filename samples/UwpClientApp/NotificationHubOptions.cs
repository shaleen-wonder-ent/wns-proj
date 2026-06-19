using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace UwpClientApp
{
    /// <summary>
    /// Configuration for the Azure Notification Hub the client registers with.
    ///
    /// To keep secrets out of source control, the recommended approach is to provide a
    /// <c>notificationhub.local.json</c> file next to the executable (it is copied to the
    /// output folder and should be git-ignored). Example contents:
    ///
    /// <code>
    /// {
    ///   "HubName": "wns-demo-hub",
    ///   "HubListenConnectionString": "Endpoint=sb://...;SharedAccessKeyName=DefaultListenSharedAccessSignature;SharedAccessKey=..."
    /// }
    /// </code>
    ///
    /// Use the <b>DefaultListenSharedAccessSignature</b> (listen-only) connection string
    /// from the hub's Access Policies page.
    /// </summary>
    internal sealed class NotificationHubOptions
    {
        private const string ConfigFileName = "notificationhub.local.json";

        public string HubName { get; set; } = string.Empty;

        public string HubListenConnectionString { get; set; } = string.Empty;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(HubName) &&
            !string.IsNullOrWhiteSpace(HubListenConnectionString);

        /// <summary>
        /// Loads options from the local config file in the app's base directory.
        /// Returns an empty (unconfigured) instance if the file is missing or invalid.
        /// </summary>
        public static NotificationHubOptions Load()
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
                if (!File.Exists(path))
                {
                    Debug.WriteLine($"[NotificationHubOptions] Config file not found: {path}");
                    return new NotificationHubOptions();
                }

                string json = File.ReadAllText(path);
                NotificationHubOptions? options = JsonSerializer.Deserialize<NotificationHubOptions>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return options ?? new NotificationHubOptions();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[NotificationHubOptions] Failed to load config: " + ex.Message);
                return new NotificationHubOptions();
            }
        }
    }
}
