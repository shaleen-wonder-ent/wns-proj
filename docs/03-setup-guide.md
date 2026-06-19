# 3. Step‑by‑step setup guide

This walks you through everything from zero to a working push notification, following the
[official Microsoft tutorial](https://learn.microsoft.com/en-us/azure/notification-hubs/notification-hubs-windows-store-dotnet-get-started-wns-push-notification).

There are **four** stages:

1. [Register your app in Partner Center (get WNS credentials)](#stage-1--register-your-app-in-partner-center)
2. [Create the Azure Notification Hub and configure WNS](#stage-2--create-the-notification-hub)
3. [Build & run the client app (register a device)](#stage-3--run-the-client-app)
4. [Send a notification (test + backend)](#stage-4--send-a-notification)

---

## Prerequisites

- An **Azure subscription**.
- **Windows 10/11** machine.
- **Visual Studio 2022 or later** with the **Universal Windows Platform development**
  workload installed (required to build the UWP client).
- A **Microsoft account** with access to [Partner Center](https://partner.microsoft.com/dashboard).
- On your machine: **Settings → System → Notifications** → ensure "Get notifications from
  apps and other senders" is **On**.

---

## Stage 1 — Register your app in Partner Center

WNS needs to know your app's identity. You get that from the Windows Store registration.

> ℹ️ **Updated links.** The old `partner.microsoft.com/dashboard/windows/first-run-experience`
> URL is deprecated. Use the current entry points below.

0. **(One‑time) Open a developer account** if you don't have one yet:
   [Open a developer account](https://learn.microsoft.com/en-us/windows/apps/publish/partner-center/open-a-developer-account).
   **Individual** registration is now **free** (the previous one‑time fee was removed);
   company accounts may still require verification.
1. Go to the **Partner Center dashboard**: <https://partner.microsoft.com/dashboard>
   and sign in with your Microsoft account. Open the **Apps and games** workspace.
2. Select **+ New product**. In the dropdown you'll see several product types —
   **choose `MSIX or PWA app`**. This is the right type for a UWP app and is what
   provides the **Package SID / Identity** values WNS needs.

   | Menu option | Use it for? |
   | --- | --- |
   | **MSIX or PWA app** | ✅ **This one** — UWP / MSIX‑packaged apps (our sample). |
   | EXE or MSI app | Unpackaged Win32 installers — no Store identity for classic WNS. |
   | GDK game | Xbox/PC games using the Game Development Kit. |
   | MSIX or PWA game | Games distributed as MSIX/PWA. |

3. Type a name and select **Reserve product name**. This creates a Store registration.
   See [Reserve your app's name](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/reserve-your-apps-name)
   for screenshots.
4. Expand **Product management → Product Identity**. **Write down** these four values:
   - **Package SID**
   - **Package/Identity/Name**
   - **Package/Identity/Publisher**
   - **Package/Properties/PublisherDisplayName**
5. Under **Product management → WNS/MPNS**, select **App Registration portal**.
   (This opens the Entra / Azure AD app registration for your Store app.)
6. On that app registration's **Overview** page, find the **Essentials** panel (the box at
   the top). On its right side select **Client credentials: Add a certificate or secret** —
   or, equivalently, use the left menu **Manage → Certificates & secrets**.
7. On **Certificates & secrets → Client secrets**, select **+ New client secret**, set a
   description/expiry, then **Add**.
   - ⚠️ **Copy the secret `Value` immediately** (not the Secret ID) — you can't see it
     again after leaving the page. This is the **Security Key / Application Secret**.

You now have the two credentials the hub needs: **Package SID** and **Client secret
(Security Key)**.

> 🔒 The Package SID and client secret are sensitive. Never put them in client code or
> commit them to git.

---

## Stage 2 — Create the Notification Hub

### Option A — Azure Portal (recommended for first time)

1. Sign in to the [Azure portal](https://portal.azure.com) and select your subscription.
2. Search for **Notification Hubs** → **Create**.
3. On the **Basics** tab:
   - **Subscription**: your subscription.
   - **Resource group**: create `rg-wns-demo` (or reuse one).
   - **Namespace**: a globally unique name, e.g. `wns-demo-ns-<your-initials>`.
   - **Notification Hub**: e.g. `wns-demo-hub`.
   - **Location**: pick one near you, e.g. `East US`.
   - **Pricing tier**: **Free** is fine for testing.
4. Select **Review + create → Create**, then **Go to resource**.

### Configure WNS on the hub

1. In your hub, under **Settings → Windows (WNS)**.
2. Enter:
   - **Package SID** — Partner Center displays it as `S-1-15-2-…`, **but you must add the
     `ms-app://` prefix here.** So if Partner Center shows
     `S-1-15-2-52517922-…`, enter `ms-app://S-1-15-2-52517922-…`.
     Forgetting the prefix is the #1 cause of the `Invalid WNS credentials` error on Save.
   - **Security Key** — the client secret **Value** from Stage 1 (the long string, **not**
     the Secret ID GUID).
3. **Save**. If it errors with `Invalid WNS credentials`, double‑check the `ms-app://`
   prefix, confirm you used the secret **Value** (not Secret ID), and retry after a few
   minutes (a new secret can take a short while to propagate).

### Get the connection strings

In the hub, open **Manage → Access Policies**. Copy:

| Policy | Connection string | Used by |
| --- | --- | --- |
| `DefaultListenSharedAccessSignature` | listen‑only | the **client app** |
| `DefaultFullSharedAccessSignature` | full access | the **backend sender** |

### Option B — Azure CLI (scripted)

```powershell
# Sign in to the right tenant + subscription
az login --tenant <your-tenant-id>
az account set --subscription <your-subscription-id>

# Variables
$rg   = "rg-wns-demo"
$loc  = "eastus"
$ns   = "wns-demo-ns-<your-initials>"   # must be globally unique
$hub  = "wns-demo-hub"

# Resource group
az group create --name $rg --location $loc

# Namespace + hub (requires the notification-hub CLI extension)
az extension add --name notification-hub --only-show-errors
az notification-hub namespace create --resource-group $rg --namespace-name $ns --location $loc --sku Free
az notification-hub create --resource-group $rg --namespace-name $ns --name $hub --location $loc

# Configure WNS credentials on the hub
az notification-hub credential wns update `
  --resource-group $rg --namespace-name $ns --notification-hub-name $hub `
  --package-sid "<YOUR_PACKAGE_SID>" `
  --secret-key  "<YOUR_CLIENT_SECRET>"

# Get the connection strings
az notification-hub authorization-rule list-keys `
  --resource-group $rg --namespace-name $ns --notification-hub-name $hub `
  --name DefaultListenSharedAccessSignature --query primaryConnectionString -o tsv

az notification-hub authorization-rule list-keys `
  --resource-group $rg --namespace-name $ns --notification-hub-name $hub `
  --name DefaultFullSharedAccessSignature --query primaryConnectionString -o tsv
```

---

## Stage 3 — Run the client app

The full source is in [`samples/UwpClientApp`](../samples/UwpClientApp). See its
[README](../samples/UwpClientApp/README.md) for details. Summary:

1. Open the project in **Visual Studio 2022 or later**.
2. Right‑click the project → **Publish → Associate App with the Store**, sign in, and
   pick the app you reserved in Stage 1. This injects the correct identity into
   `Package.appxmanifest`.
3. Open [`App.xaml.cs`](../samples/UwpClientApp/App.xaml.cs) and set:
   - `HubName` → your hub name (e.g. `wns-demo-hub`).
   - `HubListenConnectionString` → the **DefaultListenSharedAccessSignature** string.
4. Press **F5**. On launch you should see a dialog: **"Registration successful: …"**.

That dialog means the device's channel URI is now stored in your hub. ✅

---

## Stage 4 — Send a notification

### Quick test (no code) — Azure Portal "Test Send"

1. In the hub → **Overview → Test Send**.
2. **Platforms** = **Windows**, **Notification Type** = **Toast**, then **Send**.
3. A toast appears on your device. 🎉

### Real send — Backend Sender app

The full source is in [`samples/BackendSender`](../samples/BackendSender). It uses the
`Microsoft.Azure.NotificationHubs` SDK and the **Full** connection string.

```powershell
cd samples/BackendSender

# Provide config (do NOT commit the real values)
Copy-Item appsettings.json appsettings.local.json
# edit appsettings.local.json: set HubName + FullConnectionString

dotnet run
```

It sends a toast to **all** registered Windows devices. See the sender
[README](../samples/BackendSender/README.md) for tags, raw payloads, and targeting.

---

## Troubleshooting

| Symptom | Likely cause / fix |
| --- | --- |
| No "Registration successful" dialog | Listen connection string or hub name wrong; app not associated with Store. |
| Test Send says *success* but no toast | Notifications disabled in Windows Settings; or `Package.appxmanifest` identity doesn't match the WNS app. |
| `401` / auth error from hub | Wrong/expired SAS connection string. |
| WNS `401` in hub logs | Package SID or client secret on the hub is wrong/expired — re‑enter in **Windows (WNS)**. |
| Toast only works in debug, not packaged | Ensure the published package uses the same identity as registered in Partner Center. |

➡️ Back to [README](../README.md)
