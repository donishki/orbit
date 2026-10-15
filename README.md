# Orbit

A portable C# / WPF website launcher for Windows, created by **Dario P.**

## Features

- Website cards with automatic or custom icons
- Collections, search, and drag-to-reorder
- Import/export libraries with custom icons
- Undo shortcut deletion and open collections together
- Light and charcoal themes
- Starts maximized; green outer outlines indicate dragging and focus

## Run

Download the portable Windows x64 package from this repository's Releases page,
extract it, and run `ShortcutsPanel.exe`. No installer or separate .NET runtime
is required. Windows 10 or Windows 11, 64-bit.

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
dotnet publish ShortcutsPanel.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o release
```

## Checks

```powershell
dotnet run --project tests/StorageChecks.csproj
dotnet run --project tests/FeatureChecks.csproj
dotnet run --project tests/ThemeChecks.csproj
```

Theme checks include live icon downloads; unavailable sites use fallback icons.
See [README.txt](README.txt) for detailed controls and release notes.
