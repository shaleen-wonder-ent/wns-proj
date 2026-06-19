# UWP Client App — receives WNS push notifications

This is the **client** side of the sample: a Universal Windows Platform (UWP) app that
registers its device with your Azure Notification Hub and then receives toast notifications
delivered through **WNS**.

It mirrors the official Microsoft tutorial, with the registration logic in
[`App.xaml.cs`](App.xaml.cs).

---

## What it does

On launch, `InitNotificationsAsync()`:
1. Asks **WNS** for a push **channel URI**.
2. **Registers** that channel URI with your **Notification Hub** (listen‑only).
3. Shows a dialog with the **Registration ID** so you know it worked.

---

## Before you run

1. Open this project in **Visual Studio 2022** with the **Universal Windows Platform
   development** workload installed.
2. **Associate the app with the Store**: right‑click the project → **Publish → Associate
   App with the Store** → sign in → choose the app you reserved in Partner Center
   (see [docs/03-setup-guide.md](../../docs/03-setup-guide.md#stage-1--register-your-app-in-partner-center)).
   This rewrites the `<Identity>` element in `Package.appxmanifest` with the correct values.
3. In [`App.xaml.cs`](App.xaml.cs), set the two constants:

   ```csharp
   private const string HubName = "wns-demo-hub";
   private const string HubListenConnectionString =
       "Endpoint=sb://...;SharedAccessKeyName=DefaultListenSharedAccessSignature;SharedAccessKey=...";
   ```

   Use the **DefaultListenSharedAccessSignature** connection string from your hub's
   **Access Policies** page.
4. Make sure the **WindowsAzure.Messaging.Managed** NuGet package is installed (it is
   referenced in [`UwpClientApp.csproj`](UwpClientApp.csproj)). In VS: right‑click the
   solution → **Manage NuGet Packages** → confirm it restores.

---

## Run

Press **F5**. You should see **"Registration successful: &lt;id&gt;"**. Click **OK**.

Your device is now registered. Send it a notification from the Azure portal
(**Test Send**) or from the [BackendSender](../BackendSender) app.

---

## Files

| File | Purpose |
| --- | --- |
| [`App.xaml.cs`](App.xaml.cs) | App startup + the notification registration logic. |
| [`App.xaml`](App.xaml) | Application resource definition. |
| [`MainPage.xaml`](MainPage.xaml) | Minimal UI showing status. |
| [`MainPage.xaml.cs`](MainPage.xaml.cs) | Code‑behind for the page. |
| [`Package.appxmanifest`](Package.appxmanifest) | App identity & capabilities. **Updated by "Associate App with the Store".** |
| [`UwpClientApp.csproj`](UwpClientApp.csproj) | Project + NuGet references. |

> ⚠️ This project must be built with Visual Studio's UWP workload. It is **not** built by
> `dotnet build` from the CLI in this repo.
