using Microsoft.Win32;
using Orbit;
var key = @"Software\OrbitStorageChecks\" + Guid.NewGuid().ToString("N");
var folder = Path.Combine(Path.GetTempPath(), "OrbitStorageChecks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(folder);
var legacy = Path.Combine(folder, "shortcuts.json");
var dataFolder = Path.Combine(folder, "data");
void Check(bool condition, string label) { if (!condition) throw new Exception(label); Console.WriteLine("PASS " + label); }
SettingsStore Store() => new(key, legacy, dataFolder);
try
{
    using (var old = Registry.CurrentUser.CreateSubKey(key)) { old.SetValue("Library", "[{\"Name\":\"Original\"}]"); old.SetValue("DarkTheme", 1); }
    var store = Store();
    Check(store.LoadDarkTheme(), "Registry theme migrated");
    Check(store.Load()!.Contains("Original"), "Registry links migrated");
    Check(File.Exists(store.SettingsPath), "Migration creates JSON in app data directory");
    using (var old = Registry.CurrentUser.CreateSubKey(key)) old.SetValue("Library", "[]");
    Check(Store().Load()!.Contains("Original"), "JSON takes precedence after migration");
    store.Save("[{\"Name\":\"Edited\"}]");
    Check(Store().Load()!.Contains("Edited"), "Edited links persist");
    Check(Store().LoadDarkTheme(), "Link save preserves theme");
    store.SaveDarkTheme(false);
    Check(!Store().LoadDarkTheme() && Store().Load()!.Contains("Edited"), "Theme save preserves links");
    Check(File.Exists(store.SettingsPath + ".bak"), "Previous JSON backed up");
    store.Save("[]"); Check(Store().Load() == "[]", "Empty library persists");
    File.WriteAllText(store.SettingsPath, "broken");
    try { store.Save("[]"); throw new Exception("Corrupt settings overwritten"); } catch (System.Text.Json.JsonException) { }
    Check(File.ReadAllText(store.SettingsPath) == "broken", "Corrupt settings preserved");
    File.Delete(store.SettingsPath);
    Registry.CurrentUser.DeleteSubKeyTree(key, false);
    File.WriteAllText(legacy, "[]"); File.SetAttributes(legacy, FileAttributes.ReadOnly);
    Check(Store().Load() == "[]", "Read-only sidecar migration");
    Check(File.ReadAllText(legacy) == "[]", "Legacy file untouched");
}
finally
{
    Registry.CurrentUser.DeleteSubKeyTree(key, false);
    if (File.Exists(legacy)) { File.SetAttributes(legacy, FileAttributes.Normal); File.Delete(legacy); }
    if (Directory.Exists(dataFolder)) { foreach (var file in Directory.GetFiles(dataFolder)) File.Delete(file); Directory.Delete(dataFolder); }
    Directory.Delete(folder);
}
