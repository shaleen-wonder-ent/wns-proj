# .NET 9 Upgrade Report

## Project target framework modifications

| Project name          | Old Target Framework     | New Target Framework            | Commits                       |
|:----------------------|:------------------------:|:-------------------------------:|-------------------------------|
| UwpClientApp.csproj   | .NETCore,Version=v5.0 (UAP 10.0) | net9.0-windows10.0.22621.0 | e7e793ab, 71d9b0e4, c0079d52  |

This project was a UWP (Universal Windows Platform) application. It was migrated to the Windows App SDK (WinUI 3) model and the modern SDK-style project format to run on .NET 9.0.

## NuGet Packages

| Package Name                                  | Old Version | New Version | Commit Id            |
|:----------------------------------------------|:-----------:|:-----------:|----------------------|
| Microsoft.NETCore.UniversalWindowsPlatform    |   6.2.14    |  (removed)  | 12973186             |
| WindowsAzure.Messaging.Managed                |   0.1.7.9   |  (removed)  | 12973186             |
| Microsoft.WindowsAppSDK                       |             |  2.2.0      | 71d9b0e4             |
| Microsoft.Graphics.Win2D                      |             |  1.4.0      | 71d9b0e4             |
| Microsoft.Windows.Compatibility               |             |  10.0.9     | 71d9b0e4             |

## All commits

| Commit ID   | Description                                                                                  |
|:------------|:---------------------------------------------------------------------------------------------|
| 9e738082    | Commit upgrade plan                                                                           |
| 419df454    | Update UwpClientApp to use WinUI and desktop packaging                                        |
| e7e793ab    | Migrate UwpClientApp to WinUI 3 and modern .NET SDK                                           |
| c0079d52    | Update UwpClientApp.csproj runtime identifiers to 'win-*'                                     |
| 12973186    | Remove old UWP / Notification Hubs package references from UwpClientApp.csproj                |
| 71d9b0e4    | Finalize project file: correct TFM, add Windows App SDK packages, remove UWP leftovers        |
| b754743a    | Feature 1 - XAML namespace migration complete                                                 |
| 26e530e4    | Feature 2 - Application lifecycle migration complete                                          |
| c394d4f2    | Feature 3 & 4 - Notification Hub registration + MessageDialog migration complete              |
| 2685b503    | All feature upgrades complete, removed Default.rd.xml, build succeeded                        |

## Project feature upgrades

Contains summary of modifications made to the project assets during different upgrade stages.

### UwpClientApp

Here is what changed for the project during upgrade:

- Project format upgrade: converted from the legacy MSBuild (non-SDK) UWP project format to SDK-style (`Microsoft.NET.Sdk`); retargeted from UAP 10.0 (`.NETCore,Version=v5.0`) to `net9.0-windows10.0.22621.0`; added `UseWinUI`, `OutputType=WinExe`, `Platforms`, valid `RuntimeIdentifiers` (`win-x86;win-x64;win-arm64`), and `ApplicationManifest`. Removed UWP-only properties (`TargetPlatformIdentifier=UAP`, `ProjectTypeGuids`, `UseDotNetNativeToolchain`, `Prefer32Bit`, legacy WindowsXaml targets import) and per-configuration property groups.
- NuGet packages: removed `Microsoft.NETCore.UniversalWindowsPlatform` (UWP meta-package) and `WindowsAzure.Messaging.Managed` (incompatible, no supported version). Added `Microsoft.WindowsAppSDK` 2.2.0, `Microsoft.Graphics.Win2D` 1.4.0, and `Microsoft.Windows.Compatibility` 10.0.9.
- XAML namespace migration: code-behind switched from `Windows.UI.Xaml.*` to `Microsoft.UI.Xaml.*` (WinUI 3). Added the `XamlControlsResources` merged dictionary to `App.xaml` so default control styles are available.
- Application lifecycle migration: `App.xaml.cs` updated to the WinUI 3 lifecycle. `OnLaunched` now uses `Microsoft.UI.Xaml.LaunchActivatedEventArgs`, creates and activates an explicit `MainWindow` (replacing `Window.Current`), and captures the window handle (HWND) via `WinRT.Interop.WindowNative`. A `MainWindow` shell (with a navigation `Frame` that loads `MainPage`) was introduced as part of the Windows App SDK template.
- Notification Hub registration migration: the removed `WindowsAzure.Messaging.Managed` API (`NotificationHub.RegisterNativeAsync`) was re-implemented in a new `NotificationService.cs` that calls the Azure Notification Hubs REST API directly — it parses the listen connection string, generates a SAS token, creates a registration id, and upserts a native WNS registration using the channel URI obtained from `PushNotificationChannelManager`.
- MessageDialog migration: `Windows.UI.Popups.MessageDialog` (which requires an HWND in Windows App SDK) was replaced with a WinUI 3 `ContentDialog` shown against the main window's `XamlRoot`.
- Packaging: `Package.appxmanifest` updated to target `Windows.Desktop`, with the `runFullTrust` restricted capability added for the packaged desktop app model.
- Cleanup: removed obsolete `Properties\Default.rd.xml` (.NET Native runtime directives, no longer used) and the legacy `Properties\AssemblyInfo.cs`.

## Next steps

- Set your real notification hub values in `NotificationService.cs` (`HubName` and the LISTEN connection string). Avoid committing real secrets — consider moving the connection string to configuration or a backend service.
- Validate push notification registration at runtime end-to-end (request a WNS channel, register with the hub, and send a test notification from the Azure portal or the BackendSender app). The REST-based registration replaces the previous client library and should be verified on a real device/build.
- Update `Package.appxmanifest` Identity (`Name`/`Publisher`) via "Associate App with the Store" so WNS accepts pushes for your registered app.
- Review the generated `MainWindow.xaml`/`MainWindow.xaml.cs` title-bar/template content (currently sample "Photo Lab" title and custom title bar logic) and adjust to your app's branding/navigation needs.
- Build and run the packaged app on Windows to confirm WinUI 3 UI renders and the app launches as expected.
