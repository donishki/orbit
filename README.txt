ORBIT 1.1.2 - WEBSITE SHORTCUTS
Created by Dario P.

Run ShortcutsPanel.exe on 64-bit Windows 10 or Windows 11.
No installer or separate .NET installation is required.

NEW IN 1.1
- Import / Export: share a JSON library including custom icons and collection
  order. Import supports merge (skip matching name, URL, collection) or replace.
  Your theme remains personal to this PC. Imports are validated before saving.
- Drag anywhere on a card. Drop in the upper half of a target to place
  it before that card, or the lower half to place it after. Drag collection
  buttons similarly. Right-click also offers Move earlier / Move later.
- Choose icon in Add/Edit accepts PNG, JPEG, ICO, or BMP under 5 MB. Icons are
  resized and stored inside the JSON, so they travel with exported libraries.
  Use website icon restores automatic icon detection.
- Manage collections: create, rename, or delete collections. Deleting a
  collection preserves its links in Unsorted (or Everyday if deleting Unsorted).
- Undo deletion restores deleted shortcuts while this session remains open.
  Importing a library clears undo history. Undo history is not saved on exit.
- Open collection is available in Manage collections and the right-click menu.
  Opening multiple sites asks for confirmation and opens each distinct URL once.
- About Orbit displays version 1.1.2 and author Dario P.

Add, edit, delete, search, and group website cards. Dark mode is optional.
Website icons are discovered from page metadata, then /favicon.ico.
Unavailable icons use a letter fallback.

Links, icons, card colors, collection order, and theme preference are saved in:
%LOCALAPPDATA%\Orbit\settings.json
Use Open settings in the sidebar to open this folder in Explorer.
The previous save is kept as settings.json.bak.
This is persistent per-user app storage, not a temporary folder.

Existing Orbit registry settings or a legacy shortcuts.json beside the EXE
are migrated on first use if the new JSON settings file is absent.
Legacy data is preserved. Future changes use the JSON file only.

Use Import / Export to share your library. Copying only the EXE does not include
personal changes. Exports contain website addresses and any custom icons;
review your library before sharing it. The theme is not included in exports.

The bundled .NET runtime extracts supporting files to temporary storage.
This executable is not digitally signed.

Build:
dotnet publish ShortcutsPanel.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o release

Checks:
dotnet run --project tests/StorageChecks.csproj
dotnet run --project tests/FeatureChecks.csproj
dotnet run --project tests/ThemeChecks.csproj

1.1.1: aligned sidebar rows, whole-card dragging, and the Orbit icon in About.

1.1.2: starts maximized; green outer borders for drag/focus; Dark mode is at the sidebar bottom; About credits Dario P. and 2026.
