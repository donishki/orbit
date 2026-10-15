using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ShortcutsPanel;

internal static class FeatureChecks
{
    static void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine("PASS " + name); }
    [STAThread]
    static void Main()
    {
        var directory = Path.Combine(Path.GetTempPath(), "OrbitFeatures-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var store = new SettingsStore(@"Software\OrbitTests-" + Guid.NewGuid().ToString("N"), Path.Combine(directory, "absent.json"), directory);
        var app = new App(); app.InitializeComponent();
        Check(MainWindow.LoadAppIcon().PixelWidth > 0, "About icon loads from embedded resource");
        var window = new MainWindow(store);
        object? Call(string method, params object?[] args) => typeof(MainWindow).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, args);
        try
        {
            var original = LibraryOperations.Validate([new("A", "https://a.example", "One", "#A697FF"), new("B", "https://b.example", "Two", "#60D6B2"), new("C", "https://c.example", "One", "#60D6B2")]);
            Check(original.Select(x => x.Id).Distinct().Count() == 3, "Legacy links receive unique identities");
            var moved = LibraryOperations.MoveBefore(original, original[2], original[0]);
            Check(moved.Select(x => x.Name).SequenceEqual(new[] { "C", "A", "B" }), "Card reorder preserves all entries");
            Check(LibraryOperations.MoveAfter(original, original[0], original[2]).Select(x => x.Name).SequenceEqual(new[] { "B", "C", "A" }), "Dragging after last card reaches final position");
            var order = LibraryOperations.MoveBefore(new[] { "One", "Two", "Empty" }, "Two", "One");
            Check((bool)Call("Save", moved, order)!, "Save reordered library");
            Check(store.LoadCollections().SequenceEqual(order), "Collection order and empty collection persist");
            Check(JsonSerializer.Deserialize<List<Shortcut>>(store.Load()!)![0].Name == "C", "Card order persists");
            Check((bool)Call("DeleteShortcut", original[0])!, "Delete shortcut");
            Check(JsonSerializer.Deserialize<List<Shortcut>>(store.Load()!)!.Count == 2, "Deletion saved");
            Call("UndoDelete");
            Check(JsonSerializer.Deserialize<List<Shortcut>>(store.Load()!)!.Select(x => x.Name).SequenceEqual(new[] { "C", "A", "B" }), "Undo restores original position");
            var pngPath = Path.Combine(directory, "icon.png");
            var bitmap = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgra32, null, new byte[] { 0,0,255,255, 0,255,0,255, 255,0,0,255, 255,255,255,255 }, 8);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using (var file = File.Create(pngPath)) encoder.Save(file);
            var icon = CustomIcons.FromFile(pngPath); moved[0] = moved[0] with { Icon = icon };
            var export = new LibraryExport(1, moved, order);
            var roundTrip = LibraryOperations.ParseExport(JsonSerializer.Serialize(export));
            Check(roundTrip.Links[0].Icon == icon && CustomIcons.Decode(roundTrip.Links[0].Icon!).PixelWidth > 0, "Custom icon survives export/import");
            Check(roundTrip.Collections.SequenceEqual(order) && roundTrip.Links.Select(x => x.Id).SequenceEqual(moved.Select(x => x.Id)), "Export retains identities and order");
            Check(LibraryOperations.Merge(moved, roundTrip.Links).Count == 3, "Merge skips duplicate shortcuts");
            Check(LibraryOperations.ParseExport(JsonSerializer.Serialize(original)).Links.Count == 3, "Legacy array import supported");
            foreach (var invalid in new[] { "{}", "{\"Version\":99,\"Links\":[]}", "[{\"Name\":\"Unsafe\",\"Url\":\"file:///C:/test\",\"Group\":\"One\",\"Color\":\"#FFFFFF\"}]" })
            {
                bool rejected = false; try { LibraryOperations.ParseExport(invalid); } catch { rejected = true; }
                Check(rejected, "Invalid import rejected");
            }
            Check((bool)Call("Save", roundTrip.Links, roundTrip.Collections)!, "Custom-icon library saves");
            Check(JsonSerializer.Deserialize<List<Shortcut>>(store.Load()!)![0].Icon == icon, "Custom icon persists in JSON");
        }
        finally
        {
            window.Close(); app.Shutdown();
            foreach (var file in Directory.GetFiles(directory)) File.Delete(file);
            Directory.Delete(directory);
        }
    }
}
