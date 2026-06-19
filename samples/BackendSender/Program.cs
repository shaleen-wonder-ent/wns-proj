using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.Configuration;

// ─────────────────────────────────────────────────────────────────────────────
//  Backend Sender
//  ---------------
//  A small console app that sends push notifications to all Windows devices
//  registered with your Azure Notification Hub. It uses the FULL access
//  connection string (DefaultFullSharedAccessSignature).
//
//  Configure: copy appsettings.json -> appsettings.local.json and fill in
//             HubName + FullConnectionString (the .local file is gitignored).
// ─────────────────────────────────────────────────────────────────────────────

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.local.json", optional: true)
    .Build();

string hubName = config["NotificationHub:HubName"] ?? "";
string fullConnectionString = config["NotificationHub:FullConnectionString"] ?? "";

if (string.IsNullOrWhiteSpace(hubName) ||
    string.IsNullOrWhiteSpace(fullConnectionString) ||
    hubName.StartsWith("<") || fullConnectionString.StartsWith("<"))
{
    Console.Error.WriteLine(
        "Missing configuration. Copy appsettings.json to appsettings.local.json and set\n" +
        "  NotificationHub:HubName\n" +
        "  NotificationHub:FullConnectionString  (DefaultFullSharedAccessSignature)\n");
    return 1;
}

// The message to send (first CLI arg overrides the default).
string title = "Hello from Azure Notification Hubs";
string body = args.Length > 0 ? string.Join(' ', args) : "Your WNS push notification works! 🎉";

var hub = NotificationHubClient.CreateClientFromConnectionString(fullConnectionString, hubName);

// Build a WNS "toast" notification as XML (the format WNS expects).
string toastXml =
    "<toast>" +
        "<visual>" +
            "<binding template=\"ToastGeneric\">" +
                $"<text>{System.Security.SecurityElement.Escape(title)}</text>" +
                $"<text>{System.Security.SecurityElement.Escape(body)}</text>" +
            "</binding>" +
        "</visual>" +
    "</toast>";

Console.WriteLine($"Sending toast to hub '{hubName}'...");
Console.WriteLine(toastXml);
Console.WriteLine();

try
{
    // Send to ALL registered Windows devices.
    NotificationOutcome outcome = await hub.SendWindowsNativeNotificationAsync(toastXml);

    // To target a subset, pass a tag expression, e.g.:
    //   await hub.SendWindowsNativeNotificationAsync(toastXml, "user:1234");
    //   await hub.SendWindowsNativeNotificationAsync(toastXml, new[] { "sports", "en-US" });

    Console.WriteLine("Send accepted by the hub.");
    Console.WriteLine($"  TrackingId : {outcome.TrackingId}");
    Console.WriteLine($"  State      : {outcome.State}");
    Console.WriteLine();
    Console.WriteLine("If a device is registered and online, a toast should appear shortly.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Send failed: {ex.Message}");
    return 1;
}
