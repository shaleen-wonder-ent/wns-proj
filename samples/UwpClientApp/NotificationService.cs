using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Networking.PushNotifications;

namespace UwpClientApp
{
    /// <summary>
    /// Registers this device with an Azure Notification Hub so it can receive WNS
    /// push notifications.
    ///
    /// The previous UWP implementation used the <c>WindowsAzure.Messaging.Managed</c>
    /// package (<c>Microsoft.WindowsAzure.Messaging.NotificationHub</c>), which has no
    /// supported version for the Windows App SDK / .NET 9 target. This class re-implements
    /// the same "register native" behavior directly against the Notification Hubs REST API.
    /// See: https://learn.microsoft.com/rest/api/notificationhubs/
    /// </summary>
    internal static class NotificationService
    {
        private const string ApiVersion = "2015-01";

        /// <summary>
        /// Requests a WNS channel for this device and registers it with the Azure
        /// Notification Hub so the device can receive push notifications.
        /// </summary>
        /// <param name="hostPage">The page used to host any result dialog (provides XamlRoot).</param>
        /// <param name="onStatus">Optional callback invoked with human-readable status updates.</param>
        public static async Task InitNotificationsAsync(Page hostPage, Action<string>? onStatus = null)
        {
            void Report(string message)
            {
                Debug.WriteLine("[NotificationService] " + message);
                onStatus?.Invoke(message);
            }

            try
            {
                NotificationHubOptions options = NotificationHubOptions.Load();
                if (!options.IsConfigured)
                {
                    Report("Not configured. Set HubName and HubListenConnectionString " +
                           "(see NotificationService configuration).");
                    return;
                }

                Report("Requesting WNS channel...");

                // 1. Ask WNS for a push channel (the per-device "mailbox" URI).
                //    NOTE: this requires the app to run with package identity (packaged/MSIX).
                PushNotificationChannel channel =
                    await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();

                Report("Registering channel with the notification hub...");

                // 2. Register that channel URI with the notification hub via the REST API.
                string registrationId = await RegisterNativeAsync(options, channel.Uri);

                // 3. Report the registration id so you know it worked.
                string success = "Registration successful: " + registrationId;
                Report(success);
                await ShowMessageAsync(hostPage, success);
            }
            catch (Exception ex)
            {
                string failure = "Registration failed: " + ex.Message;
                Report(failure);
                await ShowMessageAsync(hostPage, failure);
            }
        }

        /// <summary>
        /// Creates (or replaces) a native WNS registration for the supplied channel URI.
        /// Mirrors the behavior of the old <c>NotificationHub.RegisterNativeAsync</c> helper.
        /// </summary>
        private static async Task<string> RegisterNativeAsync(NotificationHubOptions options, string channelUri)
        {
            (string endpoint, string keyName, string key) = ParseConnectionString(options.HubListenConnectionString);

            // Notification Hubs uses an https endpoint; the connection string carries an sb:// scheme.
            string baseUri = endpoint
                .Replace("sb://", "https://", StringComparison.OrdinalIgnoreCase)
                .TrimEnd('/');

            using var httpClient = new HttpClient();

            // Create a new registration id from the hub.
            string createRegistrationUri =
                $"{baseUri}/{options.HubName}/registrationids/?api-version={ApiVersion}";
            string sasForCreate = CreateSasToken(createRegistrationUri, keyName, key);

            using var createRequest = new HttpRequestMessage(HttpMethod.Post, createRegistrationUri);
            createRequest.Headers.TryAddWithoutValidation("Authorization", sasForCreate);
            createRequest.Content = new StringContent(string.Empty);

            using HttpResponseMessage createResponse = await httpClient.SendAsync(createRequest);
            createResponse.EnsureSuccessStatusCode();

            // The new registration id is returned in the Location header:
            // .../registrations/<registrationId>?api-version=...
            string? registrationId = ExtractRegistrationId(createResponse.Headers.Location);
            if (string.IsNullOrEmpty(registrationId))
            {
                throw new InvalidOperationException("Notification hub did not return a registration id.");
            }

            // Upsert the native (WNS) registration body for that id.
            string upsertUri =
                $"{baseUri}/{options.HubName}/registrations/{registrationId}?api-version={ApiVersion}";
            string sasForUpsert = CreateSasToken(upsertUri, keyName, key);

            string body =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<entry xmlns=\"http://www.w3.org/2005/Atom\">" +
                "<content type=\"application/xml\">" +
                "<WindowsRegistrationDescription xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns=\"http://schemas.microsoft.com/netservices/2010/10/servicebus/connect\">" +
                "<ChannelUri>" + WebUtility.HtmlEncode(channelUri) + "</ChannelUri>" +
                "</WindowsRegistrationDescription>" +
                "</content>" +
                "</entry>";

            using var upsertRequest = new HttpRequestMessage(HttpMethod.Put, upsertUri);
            upsertRequest.Headers.TryAddWithoutValidation("Authorization", sasForUpsert);
            upsertRequest.Content = new StringContent(body, Encoding.UTF8, "application/atom+xml");

            using HttpResponseMessage upsertResponse = await httpClient.SendAsync(upsertRequest);
            upsertResponse.EnsureSuccessStatusCode();

            return registrationId;
        }

        private static string? ExtractRegistrationId(Uri? location)
        {
            if (location == null)
            {
                return null;
            }

            // Path looks like: /<hub>/registrations/<registrationId>
            string path = location.AbsolutePath.TrimEnd('/');
            int lastSlash = path.LastIndexOf('/');
            return lastSlash >= 0 && lastSlash < path.Length - 1
                ? path.Substring(lastSlash + 1)
                : null;
        }

        private static (string endpoint, string keyName, string key) ParseConnectionString(string connectionString)
        {
            string? endpoint = null;
            string? keyName = null;
            string? key = null;

            foreach (string part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = part.IndexOf('=');
                if (separator < 0)
                {
                    continue;
                }

                string name = part.Substring(0, separator).Trim();
                string value = part.Substring(separator + 1).Trim();

                if (name.Equals("Endpoint", StringComparison.OrdinalIgnoreCase))
                {
                    endpoint = value;
                }
                else if (name.Equals("SharedAccessKeyName", StringComparison.OrdinalIgnoreCase))
                {
                    keyName = value;
                }
                else if (name.Equals("SharedAccessKey", StringComparison.OrdinalIgnoreCase))
                {
                    key = value;
                }
            }

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(keyName) || string.IsNullOrEmpty(key))
            {
                throw new InvalidOperationException("Notification hub connection string is missing required values.");
            }

            return (endpoint, keyName, key);
        }

        /// <summary>
        /// Builds a Shared Access Signature token for the given resource URI, matching the
        /// scheme used by Azure Service Bus / Notification Hubs.
        /// </summary>
        private static string CreateSasToken(string resourceUri, string keyName, string key)
        {
            string encodedResource = WebUtility.UrlEncode(resourceUri).ToLowerInvariant();

            // Token valid for 1 hour.
            long expiry = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .Add(TimeSpan.FromHours(1)).TotalSeconds;

            string stringToSign = encodedResource + "\n" + expiry.ToString(CultureInfo.InvariantCulture);

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            string signature = Convert.ToBase64String(
                hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

            return string.Format(
                CultureInfo.InvariantCulture,
                "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}",
                encodedResource,
                WebUtility.UrlEncode(signature),
                expiry,
                keyName);
        }

        /// <summary>
        /// Displays an informational message using a WinUI 3 <see cref="ContentDialog"/>.
        /// In the Windows App SDK a content dialog needs an XamlRoot, so the dialog is
        /// shown against the supplied host page.
        /// </summary>
        private static async Task ShowMessageAsync(Page? hostPage, string message)
        {
            XamlRoot? xamlRoot = hostPage?.XamlRoot;
            if (xamlRoot == null)
            {
                // The page is not ready yet; status is still reported via the callback/Debug output.
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "Notifications",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = xamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}

