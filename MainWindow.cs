using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Net.Http;
namespace ShortcutsPanel;
public record Shortcut(string Name, string Url, string Group, string Color, string? Icon = null, string Id = "");
public partial class MainWindow : Window
{
    readonly SettingsStore settings;
    readonly WrapPanel cards = new();
    readonly StackPanel groups = new();
    readonly TextBlock heading = new() { FontSize = 30, FontWeight = FontWeights.SemiBold };
    static readonly Dictionary<string, Task<BitmapSource?>> logos = new();
    readonly TextBox search = new() { Width = 280, ToolTip = "Search by name, website, or collection" };
    readonly TextBlock status = new() { Foreground = Brush("#939DB8"), FontSize = 12 };
    List<Shortcut> entries = [];
    string selectedGroup = "All";
    List<string> collectionOrder = [];
    readonly Stack<(Shortcut Item, int Index)> deleted = new();
    Button? undoButton;
    bool readable = true;
    static readonly string[] Palette = ["#A697FF", "#60D6B2", "#6CB8FF", "#FFB878", "#F58EB9", "#DDD17B"];
    public MainWindow(SettingsStore? store = null)
    {
        settings = store ?? new SettingsStore();
        try { Theme.Apply(settings.LoadDarkTheme()); }
        catch { Theme.Apply(false); }
        Background = Brush("#FAF9F6"); Foreground = Brush("#252924"); FontFamily = new FontFamily("Segoe UI");
        Title = "Orbit • Shortcuts Panel"; Width = 1100; Height = 720; MinWidth = 800; MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterScreen; WindowState = WindowState.Maximized;
        var root = new Grid(); Content = root;
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });
        root.ColumnDefinitions.Add(new ColumnDefinition());
        var sidebar = new DockPanel { Margin = new Thickness(20, 32, 20, 24) };
        var sideSurface = new Border { Background = Brush("#F1F2ED"), BorderBrush = Brush("#E2E5DD"), BorderThickness = new Thickness(0,0,1,0), Child = sidebar }; root.Children.Add(sideSurface);
        var brand = new StackPanel { Margin = new Thickness(12, 0, 0, 36) };
        brand.Children.Add(new TextBlock { Text = "◉  orbit", FontSize = 27, FontWeight = FontWeights.SemiBold, Foreground = Brush("#294B3C") });
        brand.Children.Add(new TextBlock { Text = "A little more organized.", FontSize = 11, Foreground = Brush("#80877D"), Margin = new Thickness(1, 6, 0, 0) });
        DockPanel.SetDock(brand, Dock.Top); sidebar.Children.Add(brand);
        var bottom = new StackPanel();
        var bottomSurface = new Border { BorderBrush = Brush("#E2E5DD"), BorderThickness = new Thickness(0,1,0,0), Padding = new Thickness(0,18,0,0), Child = bottom }; DockPanel.SetDock(bottomSurface, Dock.Bottom); sidebar.Children.Add(bottomSurface);
        var themeSwitch = new CheckBox { Content = "Dark mode", IsChecked = Theme.IsDark, Margin = new Thickness(12, 0, 0, 0), VerticalContentAlignment = VerticalAlignment.Center };
        themeSwitch.SetResourceReference(StyleProperty, "ThemeSwitch");
        themeSwitch.Click += (_, _) =>
        {
            Theme.Apply(themeSwitch.IsChecked == true);
            try { settings.SaveDarkTheme(Theme.IsDark); }
            catch (Exception ex) { MessageBox.Show(this, $"Theme changed for this session, but Windows could not save the preference.\n\n{ex.Message}", "Theme preference"); }
        };
        bottom.Children.Add(SidebarButton("Import / Export", "\uE8B5", Transfer));
        bottom.Children.Add(SidebarButton("Collections", "\uE8F1", ManageCollections));
        bottom.Children.Add(SidebarButton("About Orbit", "\uE946", About));
        bottom.Children.Add(SidebarButton("Open settings", "\uE8B7", () =>
        {
            try { settings.OpenLocation(); }
            catch (Exception ex) { MessageBox.Show(this, "Could not open the settings folder.\n\n" + ex.Message, "Open settings"); }
        }));
        bottom.Children.Add(new Border { BorderBrush = Brush("#E2E5DD"), BorderThickness = new Thickness(0,1,0,0), Margin = new Thickness(0,14,0,0), Padding = new Thickness(0,18,0,0), Child = themeSwitch });
        var nav = new DockPanel(); sidebar.Children.Add(nav);
        var label = new TextBlock { Text = "LIBRARY", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Brush("#838B7F"), Margin = new Thickness(12, 0, 0, 14) }; DockPanel.SetDock(label, Dock.Top); nav.Children.Add(label);
        nav.Children.Add(new ScrollViewer { Content = groups, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        var main = new DockPanel { Margin = new Thickness(36, 36, 28, 24) }; Grid.SetColumn(main, 1); root.Children.Add(main);
        var top = new StackPanel(); DockPanel.SetDock(top, Dock.Top); main.Children.Add(top);
        var header = new DockPanel(); top.Children.Add(header);
        var add = Button("＋  Add website", () => Edit(null), "#294B3D"); add.Foreground = Brushes.White; add.VerticalAlignment = VerticalAlignment.Center; DockPanel.SetDock(add, Dock.Right); header.Children.Add(add);
        header.Children.Add(heading);
        top.Children.Add(new TextBlock { Text = "Your favorite places, thoughtfully collected.", FontSize = 13, Foreground = Brush("#7B8377"), Margin = new Thickness(0, 9, 0, 26) });
        search.Width = double.NaN; search.HorizontalAlignment = HorizontalAlignment.Stretch;
        search.ToolTip = "Search websites and collections";
        System.Windows.Automation.AutomationProperties.SetName(search, "Search websites");
        var searchRow = new DockPanel { Margin = new Thickness(0, 0, 8, 28) };
        var searchLabel = new TextBlock { Text = "Search", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,12,0), Foreground = Brush("#7B8377"), FontSize = 12 }; searchRow.Children.Add(searchLabel); searchRow.Children.Add(search); top.Children.Add(searchRow);
        var footer = new DockPanel { Margin = new Thickness(0, 16, 8, 0) }; DockPanel.SetDock(footer, Dock.Bottom); main.Children.Add(footer);
        undoButton = Button("Undo deletion", UndoDelete); undoButton.IsEnabled = false;
        DockPanel.SetDock(undoButton, Dock.Right); footer.Children.Add(undoButton);
        status.Foreground = Brush("#868D80"); footer.Children.Add(status);
        main.Children.Add(new ScrollViewer { Content = cards, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        search.TextChanged += (_, _) => RenderCards();
        Load(); Render();
    }
    static SolidColorBrush Brush(string color) => Theme.Brush(color);
    static Button Button(string text, Action action, string? color = null)
    {
        var b = new Button { Content = text, FontSize = 13 }; if (color != null) b.Background = Brush(color);
        b.Click += (_, _) => action(); return b;
    }
    void Load()
    {
        try
        {
            var json = settings.Load();
            entries = json == null
                ? [new("Google", "https://www.google.com", "Everyday", Palette[0]), new("YouTube", "https://www.youtube.com", "Everyday", Palette[4]), new("GitHub", "https://github.com", "Work", Palette[2]), new("Wikipedia", "https://www.wikipedia.org", "Discover", Palette[1])]
                : JsonSerializer.Deserialize<List<Shortcut>>(json) ?? throw new InvalidDataException("Empty settings.");
            entries = LibraryOperations.Validate(entries);
            collectionOrder = LibraryOperations.Collections(entries, settings.LoadCollections());
        }
        catch (Exception ex) { readable = false; entries = []; MessageBox.Show(this, $"Could not load your library. Existing data has been preserved.\n\n{ex.Message}", "Settings could not be loaded"); }
    }
    bool Save(List<Shortcut> updated, List<string>? order = null)
    {
        if (!readable) { MessageBox.Show(this, "Resolve the library startup error and restart before making changes."); return false; }
        try
        {
            updated = LibraryOperations.Validate(updated);
            var nextOrder = LibraryOperations.Collections(updated, order ?? collectionOrder);
            settings.SaveLibrary(JsonSerializer.Serialize(updated, new JsonSerializerOptions { WriteIndented = true }), nextOrder);
            entries = updated; collectionOrder = nextOrder; Render(); return true;
        }
        catch (Exception ex) { MessageBox.Show(this, $"Windows could not save changes for this user account. Your previous library has been kept.\n\n{ex.Message}", "Save failed"); return false; }
    }
    void Render()
    {
        var names = LibraryOperations.Collections(entries, collectionOrder);
        if (!names.Contains(selectedGroup)) selectedGroup = "All";
        groups.Children.Clear();
        foreach (var name in new[] { "All" }.Concat(names.Where(x => x != "All")))
        {
            var count = name == "All" ? entries.Count : entries.Count(x => x.Group == name);
            var b = Button("", () => { selectedGroup = name; Render(); }, name == selectedGroup ? "#DFE6DA" : "#F1F2ED");
            b.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            var row = new DockPanel { Width = 138 };
            var number = new TextBlock { Text = count.ToString(), Foreground = Brush("#7B8377"), FontSize = 11, VerticalAlignment = VerticalAlignment.Center }; DockPanel.SetDock(number, Dock.Right); row.Children.Add(number);
            row.Children.Add(new TextBlock { Text = name == "All" ? "▦   All websites" : "•   " + name, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis, Foreground = Brush("#374632") });
            b.Content = row; b.Margin = new Thickness(0,0,0,6); groups.Children.Add(b);
            System.Windows.Automation.AutomationProperties.SetName(b, name == "All" ? "All websites" : name);
            if (name != "All") ConfigureCollection(b, name);
        }
        RenderCards();
    }
    void RenderCards()
    {
        cards.Children.Clear();
        var filtered = entries.Where(x => (selectedGroup == "All" || x.Group == selectedGroup) && $"{x.Name} {x.Url} {x.Group}".Contains(search.Text.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var item in filtered)
        {
            var layout = new DockPanel();
            var actions = new DockPanel { Margin = new Thickness(0, 10, 0, 0) }; DockPanel.SetDock(actions, Dock.Bottom); layout.Children.Add(actions);
            var edit = Button("Edit", () => Edit(item), "#FFFFFF"); edit.Padding = new Thickness(6, 3, 6, 3); edit.Foreground = Brush("#7B8377"); edit.FontSize = 11; edit.SetResourceReference(StyleProperty, "CardButton"); DockPanel.SetDock(edit, Dock.Right); actions.Children.Add(edit);
            var tag = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            tag.Children.Add(new Border { Width = 5, Height = 5, CornerRadius = new CornerRadius(3), Background = Brush(item.Color), Margin = new Thickness(0,0,6,0) });
            tag.Children.Add(new TextBlock { Text = item.Group, Foreground = Brush("#7B8377"), FontSize = 10, MaxWidth = 130, TextTrimming = TextTrimming.CharacterEllipsis }); actions.Children.Add(tag);
            var content = new StackPanel { Width = 182 };
            var logoRow = new DockPanel { Margin = new Thickness(0,0,0,16) };
            var arrow = new TextBlock { Text = "↗", FontSize = 17, Foreground = Brush("#A2AA9B"), VerticalAlignment = VerticalAlignment.Top }; DockPanel.SetDock(arrow, Dock.Right); logoRow.Children.Add(arrow);
            var badge = new Border { Background = Brush("#F5F6F2"), CornerRadius = new CornerRadius(10), Width = 42, Height = 42, HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock { Text = System.Globalization.StringInfo.GetNextTextElement(item.Name).ToUpperInvariant(), Foreground = Brush("#53654B"), FontSize = 20, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
            logoRow.Children.Add(badge); content.Children.Add(logoRow); if (item.Icon != null) badge.Child = new Image { Source = CustomIcons.Decode(item.Icon), Width = 28, Height = 28, Stretch = Stretch.Uniform };
            else _ = ApplyLogo(badge, item.Url);
            content.Children.Add(new TextBlock { Text = item.Name, Foreground = Brush("#252D23"), FontSize = 16, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
            content.Children.Add(new TextBlock { Text = new Uri(item.Url).Host.Replace("www.", ""), Foreground = Brush("#8B9285"), FontSize = 11, Margin = new Thickness(0,5,0,0), TextTrimming = TextTrimming.CharacterEllipsis });
            var launch = Button("", () => Launch(item), "#FFFFFF"); launch.SetResourceReference(StyleProperty, "CardButton"); launch.Content = content; launch.Padding = new Thickness(0); launch.ToolTip = item.Url;
            System.Windows.Automation.AutomationProperties.SetName(launch, "Open " + item.Name); layout.Children.Add(launch);
            var card = new Border { Width = 226, Height = 188, Background = Brush("#FFFFFF"), BorderBrush = Brush("#E5E8E0"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(20,18,20,14), Margin = new Thickness(0,0,14,14), Child = layout };
            cards.Children.Add(card); ConfigureCard(card, item);
        }
        if (filtered.Count == 0) cards.Children.Add(new TextBlock { Text = entries.Count == 0 ? "A fresh start. Add your first website above." : "No websites match. Try another search or collection.", FontSize = 18, Margin = new Thickness(0, 35, 0, 35) });
        heading.Text = selectedGroup == "All" ? "All websites" : selectedGroup;
        status.Text = $"{filtered.Count} websites   ·   Opens in your default browser";
    }
    static async Task ApplyLogo(Border badge, string url)
    {
        var key = new Uri(url).GetLeftPart(UriPartial.Path);
        if (!logos.TryGetValue(key, out var task)) logos[key] = task = WebsiteIcons.Fetch(key);
        var bitmap = await task;
        if (bitmap != null) badge.Child = new Image { Source = bitmap, Width = 26, Height = 26, Stretch = Stretch.Uniform };
    }
    void Launch(Shortcut item)
    {
        try { if (!ValidUrl(item.Url, out var url)) throw new Exception("Only HTTP and HTTPS websites are supported."); Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not open website"); }
    }
    public static bool ValidUrl(string? input, out string result)
    {
        result = ""; if (string.IsNullOrWhiteSpace(input)) return false;
        var value = input.Trim(); if (!value.Contains("://")) value = "https://" + value;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https") || string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo)) return false;
        result = uri.AbsoluteUri; return true;
    }
    void Edit(Shortcut? original)
    {
        var dialog = new Window { Owner = this, Title = original == null ? "Add website" : "Edit website", Width = 480, Height = 690, ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = true };
        var panel = new StackPanel { Margin = new Thickness(28) }; dialog.Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        panel.Children.Add(new TextBlock { Text = original == null ? "A new destination." : "Make it yours.", FontSize = 26, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 18) });
        TextBox Field(string label, string value) { panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 10, 0, 6) }); var t = new TextBox { Text = value, MaxLength = label == "Website URL" ? 2048 : 60 }; panel.Children.Add(t); return t; }
        var name = Field("Name", original?.Name ?? ""); var url = Field("Website URL", original?.Url ?? ""); var group = Field("Collection", original?.Group ?? "Everyday");
        panel.Children.Add(new TextBlock { Text = "Accent color", Margin = new Thickness(0, 16, 0, 8) });
        var colors = new StackPanel { Orientation = Orientation.Horizontal }; panel.Children.Add(colors); string color = original?.Color ?? Palette[0];
        void DrawColors() { colors.Children.Clear(); foreach (var p in Palette) { var b = Button(p == color ? "✓" : "", () => { color = p; DrawColors(); }, p); b.Foreground = Brush("#10131E"); b.Width = 49; b.Height = 35; b.Padding = new Thickness(0); b.Margin = new Thickness(0, 0, 10, 0); b.ToolTip = p; colors.Children.Add(b); } } DrawColors();
        string? customIcon = original?.Icon;
        var iconRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,16,0,0) };
        var iconPreview = new Image { Width = 32, Height = 32, Margin = new Thickness(0,0,10,0) };
        var iconStatus = new TextBlock { FontSize = 11, Foreground = Brush("#7B8377"), Margin = new Thickness(0,6,0,0) };
        void ShowIcon() { iconPreview.Source = customIcon == null ? null : CustomIcons.Decode(customIcon); iconStatus.Text = customIcon == null ? "Automatic website icon" : "Custom icon included in exports"; }
        iconRow.Children.Add(iconPreview);
        iconRow.Children.Add(Button("Choose icon", () => {
            var picker = new Microsoft.Win32.OpenFileDialog { Title = "Choose a website icon", Filter = "Images|*.png;*.jpg;*.jpeg;*.ico;*.bmp" };
            if (picker.ShowDialog(dialog) != true) return;
            try { customIcon = CustomIcons.FromFile(picker.FileName); ShowIcon(); }
            catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Could not load icon"); }
        }));
        var automatic = Button("Use website icon", () => { customIcon = null; ShowIcon(); }); automatic.Margin = new Thickness(8,0,0,0); iconRow.Children.Add(automatic);
        panel.Children.Add(iconRow); panel.Children.Add(iconStatus); ShowIcon();
        var error = new TextBlock { Foreground = Brush("#A83C38"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 10) }; panel.Children.Add(error);
        var row = new StackPanel { Orientation = Orientation.Horizontal }; panel.Children.Add(row);
        var save = Button("Save website", () => {
            if (string.IsNullOrWhiteSpace(name.Text) || !ValidUrl(url.Text, out var normalized)) { error.Text = "Enter a name and a valid HTTP or HTTPS website."; return; }
            var next = entries.ToList(); var item = new Shortcut(name.Text.Trim(), normalized, string.IsNullOrWhiteSpace(group.Text) ? "Everyday" : group.Text.Trim(), color, customIcon, original?.Id ?? Guid.NewGuid().ToString("N"));
            if (original == null) next.Add(item); else next[next.FindIndex(x => x.Id == original.Id)] = item;
            if (Save(next)) dialog.Close();
        }, "#294B3D"); save.Foreground = Brushes.White; save.IsDefault = true; row.Children.Add(save);
        var cancel = Button("Cancel", dialog.Close); cancel.IsCancel = true; cancel.Margin = new Thickness(8, 0, 0, 0); row.Children.Add(cancel);
        if (original != null) { var remove = Button("Delete", () => { if (MessageBox.Show(dialog, $"Remove {original.Name}?", "Delete website", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { if (DeleteShortcut(original)) dialog.Close(); } }, "#F8E9E5"); remove.Margin = new Thickness(8, 0, 0, 0); row.Children.Add(remove); }
        dialog.ContentRendered += (_, _) => { dialog.Activate(); name.Focus(); Keyboard.Focus(name); }; dialog.ShowDialog();
    }
}
