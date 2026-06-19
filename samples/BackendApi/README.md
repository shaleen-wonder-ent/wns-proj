# Backend API — send notifications over HTTP (host on Azure App Service)

An **ASP.NET Core Web API** that sends push notifications through your Azure Notification
Hub. This is the **production‑style** version of [`BackendSender`](../BackendSender): instead
of a one‑shot console app, it exposes an HTTP endpoint your real app/frontend can call to
trigger a notification. It's designed to be hosted on **Azure App Service**.

---

## Endpoint

```
POST /api/notifications/send
Content-Type: application/json
X-Api-Key: <your api key>      # required only if Api:Key is configured

{
  "title": "Hello",
  "message": "Your order has shipped",
  "tag": "user:1234"            # optional; omit to broadcast to all devices
}
```

Response:

```json
{ "trackingId": "...", "state": "Enqueued" }
```

There's also `GET /` (health check) and **Swagger UI** at `/swagger` for easy testing.

---

## Run locally

```powershell
cd samples/BackendApi

# Provide config without committing secrets (user-secrets is gitignored too)
dotnet user-secrets init
dotnet user-secrets set "NotificationHub:HubName" "wns-demo-hub"
dotnet user-secrets set "NotificationHub:FullConnectionString" "<Full connection string>"
dotnet user-secrets set "Api:Key" "choose-a-strong-key"

dotnet run
```

Open the printed URL + `/swagger`, or call it:

```powershell
curl -X POST http://localhost:5xxx/api/notifications/send `
  -H "Content-Type: application/json" -H "X-Api-Key: choose-a-strong-key" `
  -d '{ "title": "Test", "message": "Hello from the API" }'
```

---

## Deploy to Azure App Service

See the full walkthrough in
[docs/04-test-and-publish.md](../../docs/04-test-and-publish.md#lets-publish-this--host-the-sender-on-azure-app-service).
Short version:

```powershell
cd samples/BackendApi
az login
az webapp up --runtime "DOTNETCORE:9.0" --sku B1 --name <globally-unique-app-name> --location eastus

# Configure the hub + API key as App Service application settings (NOT in code)
az webapp config appsettings set --name <app-name> --resource-group <rg> --settings `
  NotificationHub__HubName="wns-demo-hub" `
  NotificationHub__FullConnectionString="<Full connection string>" `
  Api__Key="<strong-key>"
```

> Note the **double underscore** `__` in app settings — that's how App Service maps to the
> nested `NotificationHub:HubName` configuration key.

---

## Security notes

- The `X-Api-Key` header is a **minimal** gate so random callers can't spam notifications.
  For production, prefer **Microsoft Entra ID auth** (App Service Authentication / "Easy
  Auth") or put the API behind **Azure API Management**.
- Keep the **Full** connection string in **App Service settings or Key Vault**, never in
  source. Consider a [Key Vault reference](https://learn.microsoft.com/azure/app-service/app-service-key-vault-references).
- Always serve over **HTTPS** (App Service does this by default).
