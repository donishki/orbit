using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ShortcutsPanel;

public sealed record LibraryExport(int Version, List<Shortcut> Links, List<string> Collections);

public static class LibraryOperations
{
    public static List<Shortcut> Validate(IEnumerable<Shortcut> input)
    {
        var result = new List<Shortcut>(); var ids = new HashSet<string>();
        foreach (var item in input)
        {
            if (result.Count >= 5000) throw new InvalidDataException("A library can contain at most 5,000 websites.");
            if (item == null || string.IsNullOrWhiteSpace(item.Name) || item.Name.Length > 60 || string.IsNullOrWhiteSpace(item.Group) || item.Group.Length > 60 || item.Url?.Length > 2048 || !MainWindow.ValidUrl(item.Url, out var url)) throw new InvalidDataException("Every website needs a name, collection, and valid HTTP or HTTPS URL.");
            if (item.Color == null || !Regex.IsMatch(item.Color, "^#[0-9a-fA-F]{6}$")) throw new InvalidDataException("Invalid website accent color.");
            if (item.Icon != null) CustomIcons.Decode(item.Icon);
            var id = item.Id;
            if (string.IsNullOrWhiteSpace(id) || id.Length > 64 || !ids.Add(id)) { id = Guid.NewGuid().ToString("N"); ids.Add(id); }
            result.Add(item with { Id = id, Name = item.Name.Trim(), Url = url, Group = item.Group.Trim() == "All" ? "All (collection)" : item.Group.Trim() });
        }
        return result;
    }
    public static List<string> Collections(IEnumerable<Shortcut> entries, IEnumerable<string> order) =>
        order.Concat(entries.Select(x => x.Group)).Where(x => !string.IsNullOrWhiteSpace(x) && x != "All" && x.Length <= 60).Select(x => x.Trim()).Distinct().ToList();

    public static List<T> MoveBefore<T>(IEnumerable<T> source, T moving, T target)
    {
        var list = source.ToList();
        if (EqualityComparer<T>.Default.Equals(moving, target) || !list.Contains(moving) || !list.Contains(target)) return list;
        list.Remove(moving); list.Insert(list.IndexOf(target), moving); return list;
    }
    public static List<T> MoveAfter<T>(IEnumerable<T> source, T moving, T target)
    {
        var list = source.ToList();
        if (EqualityComparer<T>.Default.Equals(moving, target) || !list.Contains(moving) || !list.Contains(target)) return list;
        list.Remove(moving); list.Insert(list.IndexOf(target) + 1, moving); return list;
    }
    public static LibraryExport ParseExport(string json)
    {
        if (json.Length > 20 * 1024 * 1024) throw new InvalidDataException("Import is too large (20 MB maximum).");
        using var document = JsonDocument.Parse(json);
        List<Shortcut>? links; List<string> order = [];
        if (document.RootElement.ValueKind == JsonValueKind.Array) links = document.RootElement.Deserialize<List<Shortcut>>();
        else
        {
            var exported = document.RootElement.Deserialize<LibraryExport>() ?? throw new InvalidDataException("Invalid library file.");
            if (exported.Version != 1) throw new InvalidDataException("This library version is not supported.");
            links = exported.Links; order = exported.Collections ?? [];
        }
        if (links == null) throw new InvalidDataException("No websites found in the import.");
        var valid = Validate(links); return new(1, valid, Collections(valid, order));
    }
    public static List<Shortcut> Merge(IEnumerable<Shortcut> existing, IEnumerable<Shortcut> incoming)
    {
        var result = existing.ToList();
        foreach (var item in incoming)
            if (!result.Any(x => x.Url == item.Url && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase) && x.Group.Equals(item.Group, StringComparison.OrdinalIgnoreCase)))
                result.Add(item with { Id = Guid.NewGuid().ToString("N") });
        return Validate(result);
    }
}
