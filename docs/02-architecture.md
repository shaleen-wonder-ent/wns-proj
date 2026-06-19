# 2. Architecture

This page shows the system from a few angles: the **components**, the **registration
flow**, and the **send flow**.

---

## Component diagram

```mermaid
flowchart TB
    subgraph Device["🖥️ User's Windows Device"]
        App["UWP Client App<br/>(samples/UwpClientApp)"]
    end

    subgraph Microsoft["☁️ Microsoft Cloud"]
        WNS["WNS<br/>Windows Push<br/>Notification Service"]
    end

    subgraph Azure["🔷 Azure Subscription"]
        subgraph NS["Notification Hubs Namespace"]
            Hub["Notification Hub<br/>(WNS credentials configured)"]
        end
    end

    subgraph Yours["🏢 Your Infrastructure"]
        Backend["Backend Sender<br/>(samples/BackendSender)"]
    end

    App -- "register channel URI<br/>(Listen SAS)" --> Hub
    App -- "request channel" --> WNS
    Backend -- "send notification<br/>(Full SAS)" --> Hub
    Hub -- "deliver via WNS" --> WNS
    WNS -- "push" --> App
```

---

## Registration flow (client side)

What happens when the **app starts up**:

```mermaid
sequenceDiagram
    autonumber
    participant App as UWP App
    participant WNS as WNS
    participant Hub as Notification Hub

    App->>WNS: CreatePushNotificationChannelForApplicationAsync()
    WNS-->>App: channel.Uri
    App->>Hub: RegisterNativeAsync(channel.Uri)
    Hub-->>App: RegistrationId
    Note over App: Show "Registration successful"
```

> The client uses the **DefaultListenSharedAccessSignature** (listen‑only) connection
> string. It can register itself but cannot send notifications to others — least privilege.

---

## Send flow (backend side)

What happens when you want to **push a message**:

```mermaid
sequenceDiagram
    autonumber
    participant Backend as Backend Sender
    participant Hub as Notification Hub
    participant WNS as WNS
    participant App as UWP App (all devices)

    Backend->>Hub: SendWindowsNativeNotificationAsync(toastXml)
    Hub->>Hub: Look up all matching registrations
    loop For each registered device
        Hub->>WNS: POST toast to channel URI
        WNS-->>App: Deliver toast 🎉
    end
    Hub-->>Backend: NotificationOutcome (tracking id)
```

> The backend uses the **DefaultFullSharedAccessSignature** (full access) connection
> string. **Keep this secret** — never ship it inside the client app.

---

## Security & credentials map

```mermaid
flowchart LR
    PartnerCenter["Partner Center<br/>(Windows Store app)"] -->|Package SID<br/>+ Client Secret| Hub["Notification Hub<br/>WNS settings"]
    Hub -->|Listen SAS| Client["Client App"]
    Hub -->|Full SAS| Backend["Backend"]
```

| Credential | Where it lives | Who uses it | Secret? |
| --- | --- | --- | --- |
| Package SID + Client Secret | Hub → WNS settings | The hub (to auth to WNS) | ✅ Yes |
| Listen SAS connection string | Client app | Client (register only) | ⚠️ Low risk |
| Full SAS connection string | Backend only | Backend (send) | ✅ Yes — never in client |

➡️ Next: [03 — Setup guide](03-setup-guide.md)
