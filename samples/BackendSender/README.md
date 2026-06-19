# Backend Sender — pushes notifications through the hub

A small **.NET 9 console app** that sends push notifications to every Windows device
registered with your Azure Notification Hub. This is the **server side** of the sample.

It uses the official [`Microsoft.Azure.NotificationHubs`](https://www.nuget.org/packages/Microsoft.Azure.NotificationHubs)
SDK and the **DefaultFullSharedAccessSignature** (full‑access) connection string.

---

## Configure

```powershell
cd samples/BackendSender
Copy-Item appsettings.json appsettings.local.json
```

Edit `appsettings.local.json` (this file is **gitignored**) and set:

```json
{
  "NotificationHub": {
    "HubName": "wns-demo-hub",
    "FullConnectionString": "Endpoint=sb://...;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=..."
  }
}
```

> 🔒 The **Full** connection string can send to everyone — keep it server‑side only.
> Never put it in the client app.

---

## Run

```powershell
# Default message
dotnet run

# Custom message (everything after `--` becomes the toast body)
dotnet run -- "Deployment finished successfully"
```

Expected output:

```
Sending toast to hub 'wns-demo-hub'...
Send accepted by the hub.
  TrackingId : ...
  State      : Enqueued
```

A toast then appears on any registered, online device.

---

## How it works

[`Program.cs`](Program.cs):
1. Loads the hub name + full connection string from config.
2. Creates a `NotificationHubClient`.
3. Builds a WNS **toast** payload as XML.
4. Calls `SendWindowsNativeNotificationAsync(...)` to broadcast to all Windows registrations.

### Targeting a subset with tags

If your client registered with tags, you can target them:

```csharp
// Single tag
await hub.SendWindowsNativeNotificationAsync(toastXml, "user:1234");

// Tag expression (devices tagged with BOTH)
await hub.SendWindowsNativeNotificationAsync(toastXml, new[] { "sports", "en-US" });
```

See [docs/01-concepts.md](../../docs/01-concepts.md) for how tags work.
