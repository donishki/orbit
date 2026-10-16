# Orbit

A portable C# / WPF website launcher for Windows.

[**Download for Windows**](https://github.com/donishki/orbit/releases/latest) · [Features](#features) · [Build from source](#build)

## Screenshots

### Charcoal

![Orbit in charcoal mode, showing website cards and the collection sidebar](docs/screenshots/orbit-dark.png)

### Light

![Orbit in light mode, showing website cards and the collection sidebar](docs/screenshots/orbit-light.png)

## Features

- Website cards with automatic or custom icons
- Collections, search, and drag-to-reorder
- Import/export libraries with custom icons
- Undo shortcut deletion and open collections together
- Light and charcoal themes
- Starts maximized; green outer outlines indicate dragging and focus

## Run

Choose a Windows x64 download from [Releases](https://github.com/donishki/orbit/releases/latest):

- **Orbit-Setup-win-x64.exe**: installs Orbit for your Windows account, adds a Start menu shortcut, and offers an optional desktop shortcut. Remove it through Windows Settings > Apps.
- **Orbit-Portable-win-x64.zip**: extract and run `Orbit.exe`, without installing.

Both include .NET and support 64-bit Windows 10 or Windows 11. The installer defaults to `%LOCALAPPDATA%\Programs\Orbit`, requires no administrator password, and preserves links and settings during upgrades and uninstall. Installed and portable copies share the same settings for your account.

## Settings

Links and preferences are stored in `%LOCALAPPDATA%\Orbit\settings.json`.
**Open settings** opens that folder. The previous save is retained as
`settings.json.bak`. Older Orbit registry or sidecar settings migrate automatically.

Use **Import / Export** to share your library. Copying the executable alone does
not carry personal links. Custom icons and collection order are included in exports;
the theme remains local. Undo history lasts for the current session.

## Build

Requires Windows and the .NET 8 SDK.

```powershell
dotnet publish Orbit.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o release
```

### Build the installer

Install Inno Setup 7, then run:

```powershell
./installer/Build-Installer.ps1
```

Use `-IsccPath` for a compiler installed elsewhere. The output is
`release/Orbit-Setup-win-x64.exe`. The installer bundles the runtime as normal
files so the installed app does not need to extract it on launch.

## Checks

```powershell
dotnet run --project tests/StorageChecks.csproj
dotnet run --project tests/FeatureChecks.csproj
dotnet run --project tests/ThemeChecks.csproj
```

Theme checks include live icon downloads; unavailable sites use fallback icons.
See [README.txt](README.txt) for detailed controls and release notes.
