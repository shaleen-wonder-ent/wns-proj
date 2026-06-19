using System.Security;
using Microsoft.Azure.NotificationHubs;

// ─────────────────────────────────────────────────────────────────────────────
//  BackendApi
//  ----------
//  An ASP.NET Core Web API that sends push notifications through an Azure
//  Notification Hub. Designed to be hosted on Azure App Service so your real
//  app/frontend can call it over HTTP to trigger a notification.
//
//  Configuration (set as App Service "Application settings" in production):
//    NotificationHub:HubName               -> e.g. "wns-demo-hub"
//    NotificationHub:FullConnectionString  -> DefaultFullSharedAccessSignature
//    Api:Key                               -> a shared secret callers must send
//                                             in the "X-Api-Key" header (optional
//                                             but strongly recommended)
// ─────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string? hubName = builder.Configuration["NotificationHub:HubName"];
string? fullConnectionString = builder.Configuration["NotificationHub:FullConnectionString"];
string? apiKey = builder.Configuration["Api:Key"];

// Register a single NotificationHubClient for the app's lifetime.
builder.Services.AddSingleton(_ =>
{
    if (string.IsNullOrWhiteSpace(hubName) || string.IsNullOrWhiteSpace(fullConnectionString))
    {
        throw new InvalidOperationException(
            "NotificationHub:HubName and NotificationHub:FullConnectionString must be configured.");
    }
    return NotificationHubClient.CreateClientFromConnectionString(fullConnectionString, hubName);
});

var app = builder.Build();

// Swagger UI is handy for testing; keep it on in this sample.
app.UseSwagger();
app.UseSwaggerUI();

// Simple health/landing endpoint.
app.MapGet("/", () => Results.Ok(new { status = "ok", message = "BackendApi is running. POST /api/notifications/send" }));

// Send a Windows (WNS) toast notification through the hub.
//   Body: { "title": "...", "message": "...", "tag": "optional-tag" }
//   If "tag" is omitted, the toast is broadcast to ALL registered Windows devices.
app.MapPost("/api/notifications/send", async (SendRequest req, NotificationHubClient hub, HttpRequest http) =>
{
    // Lightweight auth gate. If an Api:Key is configured, callers must match it.
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        if (!http.Headers.TryGetValue("X-Api-Key", out var provided) ||
            !string.Equals(provided, apiKey, StringComparison.Ordinal))
        {
            return Results.Unauthorized();
        }
    }

    if (string.IsNullOrWhiteSpace(req.Message))
    {
        return Results.BadRequest(new { error = "message is required" });
    }

    string title = string.IsNullOrWhiteSpace(req.Title) ? "Notification" : req.Title;

    // Build the WNS toast XML payload.
    string toastXml =
        "<toast>" +
            "<visual>" +
                "<binding template=\"ToastGeneric\">" +
                    $"<text>{SecurityElement.Escape(title)}</text>" +
                    $"<text>{SecurityElement.Escape(req.Message)}</text>" +
                "</binding>" +
            "</visual>" +
        "</toast>";

    NotificationOutcome outcome = string.IsNullOrWhiteSpace(req.Tag)
        ? await hub.SendWindowsNativeNotificationAsync(toastXml)              // broadcast
        : await hub.SendWindowsNativeNotificationAsync(toastXml, req.Tag);    // targeted

    return Results.Ok(new { outcome.TrackingId, state = outcome.State.ToString() });
});

app.Run();

// Request body for the send endpoint.
record SendRequest(string? Title, string Message, string? Tag);
