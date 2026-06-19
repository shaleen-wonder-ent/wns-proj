# .NET 9.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 9.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 9.0 upgrade.
3. Upgrade UwpClientApp.csproj

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

Table below contains projects that do belong to the dependency graph for selected projects and should not be included in the upgrade.

| Project name   | Description         |
|:---------------|:-------------------:|
| *(none)*       | All projects included |

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                  | Current Version | New Version | Description                                                                                          |
|:----------------------------------------------|:---------------:|:-----------:|:-----------------------------------------------------------------------------------------------------|
| Microsoft.Graphics.Win2D                      |                 |  1.4.0      | Replacement for Microsoft.NETCore.UniversalWindowsPlatform (Win2D graphics for Windows App SDK)      |
| Microsoft.NETCore.UniversalWindowsPlatform    |   6.2.14        |             | Remove - UWP meta-package; replaced by Windows App SDK packages                                      |
| Microsoft.Windows.Compatibility               |                 |  10.0.9     | Replacement for Microsoft.NETCore.UniversalWindowsPlatform (Windows compatibility APIs)              |
| Microsoft.WindowsAppSDK                       |                 |  2.2.0      | Replacement for Microsoft.NETCore.UniversalWindowsPlatform (WinUI 3 / Windows App SDK runtime)       |
| WindowsAzure.Messaging.Managed                |   0.1.7.9       |             | Remove - incompatible, no supported version found. Notification Hub client registration must be re-implemented |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### UwpClientApp.csproj modifications

This is a UWP (Universal Windows Platform) project that must be migrated to the **Windows App SDK (WinUI 3)** model to run on .NET 9.0. This is a significant platform migration, not just a target framework bump.

Project properties changes:
  - Convert the project from the legacy MSBuild (non-SDK) format to **SDK-style** project format.
  - Target framework should be changed from `.NETCore,Version=v5.0` (UAP 10.0) to `net9.0-windows10.0.22621.0`.
  - Add `<UseWinUI>true</UseWinUI>`, set `<OutputType>WinExe</OutputType>`, `<Platforms>x86;x64;ARM64</Platforms>`, `<RuntimeIdentifiers>` and `<TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>`.
  - Remove UWP-specific properties (`TargetPlatformIdentifier=UAP`, `ProjectTypeGuids`, `UseDotNetNativeToolchain`, `ProjectGuid`, `Prefer32Bit`, legacy WindowsXaml targets import).

NuGet packages changes:
  - Microsoft.NETCore.UniversalWindowsPlatform should be removed (UWP meta-package not used in Windows App SDK).
  - Microsoft.WindowsAppSDK `2.2.0` should be added (*WinUI 3 runtime*).
  - Microsoft.Graphics.Win2D `1.4.0` should be added (*Win2D graphics replacement*).
  - Microsoft.Windows.Compatibility `10.0.9` should be added (*Windows compatibility APIs*).
  - WindowsAzure.Messaging.Managed should be removed (*incompatible, no supported version found*).

Feature upgrades:
  - **XAML namespace migration**: Replace `Windows.UI.Xaml.*` namespaces with `Microsoft.UI.Xaml.*` (WinUI 3) in `App.xaml.cs`, `MainPage.xaml.cs`, `App.xaml`, and `MainPage.xaml`.
  - **Application lifecycle migration**: Update `App.xaml.cs` to the WinUI 3 lifecycle. `OnLaunched(LaunchActivatedEventArgs)` uses `Microsoft.UI.Xaml.LaunchActivatedEventArgs`; replace `Window.Current` usage with an explicit `Microsoft.UI.Xaml.Window` instance and a root `Frame`.
  - **Notification Hub registration migration**: `WindowsAzure.Messaging.Managed` (Microsoft.WindowsAzure.Messaging.NotificationHub) has no supported replacement package. Re-implement device registration with the Azure Notification Hubs REST API, or move registration to a backend service. The existing `NotificationHub.RegisterNativeAsync` call and `PushNotificationChannelManager` usage in `App.xaml.cs` must be re-worked accordingly.
  - **MessageDialog migration**: `Windows.UI.Popups.MessageDialog` requires a window handle (HWND) in Windows App SDK. Replace with a WinUI 3 `ContentDialog` (with `XamlRoot` set) or initialize the dialog with the window handle via `WinRT.Interop`.

Other changes:
  - Update `Package.appxmanifest` if needed for the Windows App SDK packaging model (the project remains a packaged Windows app).
  - The `UwpClientApp_TemporaryKey.pfx`, `Package.StoreAssociation.xml`, `Default.rd.xml` and asset references should be reviewed; `Default.rd.xml` (.NET Native runtime directives) is no longer used and can be removed.
