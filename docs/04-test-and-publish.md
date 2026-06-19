# 4. Test & Publish

Now that the hub is configured, this page covers three things:

1. [Let's test this](#lets-test-this-end-to-end) — run the whole flow end‑to‑end.
2. [Let's publish this](#lets-publish-this--host-the-sender-on-azure-app-service) — host the
   **sender** as an API (we use **Azure App Service**, with alternatives compared).
3. [Publish the Windows app to the Store](#publish-the-windows-app-to-the-microsoft-store) —
   distribute the **client** app.

Recap of the two pieces (see [01-concepts.md](01-concepts.md)):

- **UwpClientApp** = the *receiver*, installed **on each Windows device**.
- **BackendSender** / **BackendApi** = the *sender*, runs **on a server** (or your PC).

---

## Let's test this (end‑to‑end)

Goal: get a real toast to appear on your machine.

### Step 1 — Register a device (run the client)
1. Open [`samples/UwpClientApp`](../samples/UwpClientApp) in **Visual Studio 2022 or later**.
2. In [`App.xaml.cs`](../samples/UwpClientApp/App.xaml.cs) set `HubName` and
   `HubListenConnectionString` (the **Listen** connection string).
3. **Publish → Associate App with the Store** → pick your reserved app.
4. Press **F5**. You should see **"Registration successful: …"**. ✅
   Your device's channel URI is now stored in the hub.

### Step 2 — Quick test from the portal (no code)
1. Azure portal → your hub → **Overview → Test Send**.
2. **Platforms = Windows**, **Notification Type = Toast** → **Send**.
3. A toast appears on your device. 🎉 This proves WNS + the hub are wired correctly.

### Step 3 — Send programmatically (the real way)
Pick **one** of the senders:

**Option A — console app ([`BackendSender`](../samples/BackendSender)):**
```powershell
cd samples/BackendSender
Copy-Item appsettings.json appsettings.local.json   # then edit it: HubName + FullConnectionString
dotnet run -- "Hello from my backend"
```

**Option B — Web API ([`BackendApi`](../samples/BackendApi)):**
```powershell
cd samples/BackendApi
dotnet user-secrets set "NotificationHub:HubName" "wns-demo-hub"
dotnet user-secrets set "NotificationHub:FullConnectionString" "<Full connection string>"
dotnet run
# then POST to /api/notifications/send (or use /swagger)
```

### Test checklist
- [ ] Client shows "Registration successful".
- [ ] Portal **Test Send** delivers a toast.
- [ ] `BackendSender` / `BackendApi` delivers a toast.

If a toast doesn't arrive, see [Troubleshooting](03-setup-guide.md#troubleshooting).

---

## Let's publish this — host the sender on Azure App Service

In production the **sender** lives on a server so your app can trigger notifications. Your
choice — **App Service hosting [`BackendApi`](../samples/BackendApi) as an HTTP API** — is a
great default.

### Which host? (quick comparison)

| Option | Best when | Notes |
| --- | --- | --- |
| **App Service (Web API)** ✅ | You want an always‑on HTTP endpoint your app calls. | Simplest; built‑in HTTPS, auth, scaling, slots. **We use this.** |
| **Azure Functions** | Sends are event/timer‑driven (queue message, schedule). | Pay‑per‑use; great for "send on event". |
| **Container Apps** | You're already containerized / want scale‑to‑zero microservices. | More moving parts than needed for one endpoint. |

### Deploy `BackendApi` to App Service

```powershell
cd samples/BackendApi
az login
az account set --subscription <your-subscription-id>

# Create + deploy in one step (creates the resource group, plan, and web app)
az webapp up `
  --runtime "DOTNETCORE:9.0" `
  --sku B1 `
  --name <globally-unique-app-name> `
  --resource-group rg-wns-demo `
  --location eastus
```

### Configure secrets as App Service application settings
Never bake the connection string into the image/source. Set them as app settings:

```powershell
az webapp config appsettings set `
  --name <app-name> --resource-group rg-wns-demo --settings `
  NotificationHub__HubName="wns-demo-hub" `
  NotificationHub__FullConnectionString="<Full connection string>" `
  Api__Key="<choose-a-strong-key>"
```

> The **double underscore** `__` maps to the nested config keys
> (`NotificationHub:HubName`, etc.). For extra hardening, store the connection string in
> **Key Vault** and use a
> [Key Vault reference](https://learn.microsoft.com/azure/app-service/app-service-key-vault-references).

### Call your hosted API
```powershell
curl -X POST https://<app-name>.azurewebsites.net/api/notifications/send `
  -H "Content-Type: application/json" -H "X-Api-Key: <your-key>" `
  -d '{ "title": "Shipped", "message": "Your order is on the way" }'
```

A toast appears on every registered Windows device. Your real frontend/service would call
this same endpoint.

### Hardening for production
- Replace the `X-Api-Key` gate with **Microsoft Entra ID auth** (App Service "Easy Auth")
  or front it with **Azure API Management**.
- Keep secrets in **Key Vault**; enable **managed identity** on the web app.
- Restrict CORS / network access to just your callers.
- Serve over **HTTPS only** (default on App Service).

> Prefer **Azure Functions** instead? The same ~10 lines of send logic drop into an
> HTTP‑triggered or timer‑triggered function. Ask and we'll add a `BackendFunction` sample.

---

## Publish the Windows app to the Microsoft Store

The **client** app installs on user devices. For a demo you just F5‑deploy to your own PC.
To distribute it to others, publish to the **Microsoft Store**:

1. In **Visual Studio**, right‑click the project → **Publish → Create App Packages…**.
2. Choose **Microsoft Store using your developer account** and sign in.
3. Select the app you reserved in
   [Stage 1](03-setup-guide.md#stage-1--register-your-app-in-partner-center).
4. Pick architectures (x64/ARM64), build the **`.msixupload`** package.
5. Go to **Partner Center → your app → Submissions → Packages**, upload the
   `.msixupload`, complete **Store listing**, **Pricing & availability**, **Age ratings**,
   then **Submit** for certification.
6. After it passes certification, the app goes live and users can install it (and start
   receiving your notifications).

> Alternative (no Store): **sideload** the `.msix` to specific machines — useful for
> internal/enterprise apps. See
> [Sideload apps](https://learn.microsoft.com/windows/apps/package-and-deploy/sideload-apps).

➡️ Back to [README](../README.md)
