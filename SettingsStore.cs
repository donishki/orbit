using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace Orbit;

public sealed class SettingsStore
{
    readonly string registryPath;
    readonly string legacyPath;
    public string Location { get; }
    public string SettingsPath => Path.Combine(Location, "settings.json");
    public sealed class Data
    {
        public int Version { get; set; } = 1;
        public JsonElement? Links { get; set; }
        public bool DarkTheme { get; set; }
        public List<string> Collections { get; set; } = [];
    }
    public SettingsStore(string registryPath = @"Software\OrbitShortcutsPanel", string? legacyPath = null, string? directory = null)
    {
        this.registryPath = registryPath;
        this.legacyPath = legacyPath ?? Path.Combine(AppContext.BaseDirectory, "shortcuts.json");
        Location = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Orbit");
    }
    public void OpenLocation()
    {
        Directory.CreateDirectory(Location);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", "\"" + Location + "\"") { UseShellExecute = true });
    }
    Data Read()
    {
        if (File.Exists(SettingsPath))
        {
            var data = JsonSerializer.Deserialize<Data>(File.ReadAllText(SettingsPath)) ?? throw new InvalidDataException("Empty settings file.");
            if (data.Version != 1) throw new InvalidDataException("Unsupported settings version.");
            if (data.Links is JsonElement links && links.ValueKind != JsonValueKind.Array) throw new InvalidDataException("Invalid links in settings.");
            return data;
        }
        var migrated = new Data();
        // Only read the registry for migration; new changes are always saved to JSON.
        using var key = Registry.CurrentUser.OpenSubKey(registryPath);
        var previous = key?.GetValue("Library");
        if (previous != null && previous is not string) throw new InvalidDataException("Invalid legacy library type.");
        string? json = previous as string;
        if (json == null && File.Exists(legacyPath)) json = File.ReadAllText(legacyPath);
        if (json != null) migrated.Links = ParseLinks(json);
        migrated.DarkTheme = key?.GetValue("DarkTheme") is int value && value == 1;
        if (json != null || key?.GetValue("DarkTheme") != null) Write(migrated);
        return migrated;
    }
    static JsonElement ParseLinks(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array) throw new InvalidDataException("Links must be a JSON array.");
        return document.RootElement.Clone();
    }
    void Write(Data data)
    {
        Directory.CreateDirectory(Location);
        var temporary = Path.Combine(Location, "settings-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            if (File.Exists(SettingsPath)) File.Replace(temporary, SettingsPath, SettingsPath + ".bak");
            else File.Move(temporary, SettingsPath);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public string? Load() => Read().Links?.GetRawText();
    public bool LoadDarkTheme() => Read().DarkTheme;
    public List<string> LoadCollections() => Read().Collections ?? [];
    public void SaveLibrary(string json, List<string> collections)
    {
        var data = Read(); data.Links = ParseLinks(json); data.Collections = collections; Write(data);
    }
    public void SaveDarkTheme(bool dark) { var data = Read(); data.DarkTheme = dark; Write(data); }
    public void Save(string json) { var data = Read(); data.Links = ParseLinks(json); Write(data); }
}
