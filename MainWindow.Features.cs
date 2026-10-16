using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Orbit;

public partial class MainWindow
{
    static Button SidebarButton(string title, string glyph, Action action)
    {
        var button = Button("", action, "#F1F2ED");
        button.Padding = new Thickness(12,10,12,10); button.Margin = new Thickness(0,2,0,2);
        var row = new StackPanel { Orientation = Orientation.Horizontal, Width = 138 };
        row.Children.Add(new TextBlock { Text = glyph, FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 14, Width = 24, VerticalAlignment = VerticalAlignment.Center, Foreground = Brush("#7B8377") });
        row.Children.Add(new TextBlock { Text = title, FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
        button.Content = row;
        System.Windows.Automation.AutomationProperties.SetName(button, title);
        return button;
    }
    Window Dialog(string title, out StackPanel panel, double height = 420)
    {
        var window = new Window { Owner = this, Title = title, Width = 500, Height = height, MinHeight = 300, MinWidth = 420, WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = true };
        panel = new StackPanel { Margin = new Thickness(28) };
        window.Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        panel.Children.Add(new TextBlock { Text = title, FontSize = 26, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,0,0,18) });
        return window;
    }
    void About()
    {
        var dialog = Dialog("About Orbit", out var panel, 380);
        var title = (TextBlock)panel.Children[0]; panel.Children.RemoveAt(0);
        title.Margin = new Thickness(0); title.VerticalAlignment = VerticalAlignment.Center;
        var header = new DockPanel { Margin = new Thickness(0,0,0,20) };
        var logo = new Image { Source = LoadAppIcon(), Width = 48, Height = 48, Margin = new Thickness(16,0,0,0) };
        DockPanel.SetDock(logo, Dock.Right); header.Children.Add(logo); header.Children.Add(title); panel.Children.Insert(0, header);
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        panel.Children.Add(new TextBlock { Text = $"Version {version?.Major}.{version?.Minor}.{version?.Build}", FontSize = 14, Margin = new Thickness(0,0,0,20) });
        panel.Children.Add(new TextBlock { Text = "Created by Dario P. • 2026", FontSize = 19, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = "Your favorite websites, organized in one place.\nA portable Windows launcher with collections, custom icons, and light and dark themes.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,16,0,24), Foreground = Brush("#7B8377") });
        panel.Children.Add(Button("Close", dialog.Close)); dialog.ShowDialog();
    }
    public static System.Windows.Media.Imaging.BitmapSource LoadAppIcon()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("OrbitIcon") ?? throw new IOException("Missing embedded icon.");
        var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(stream, System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0]; frame.Freeze(); return frame;
    }
    void Transfer()
    {
        var dialog = Dialog("Import / Export", out var panel, 430);
        panel.Children.Add(new TextBlock { Text = "Share your websites, collections, order, and custom icons in a single JSON file. Your theme stays personal to this PC.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,20) });
        panel.Children.Add(Button("Export library…", () => {
            if (!readable) { MessageBox.Show(dialog, "Resolve the startup library error first."); return; }
            var picker = new Microsoft.Win32.SaveFileDialog { Title = "Export Orbit library", FileName = "Orbit-library.json", Filter = "Orbit library|*.json", DefaultExt = ".json" };
            if (picker.ShowDialog(dialog) != true) return;
            try { File.WriteAllText(picker.FileName, JsonSerializer.Serialize(new LibraryExport(1, entries, collectionOrder), new JsonSerializerOptions { WriteIndented = true })); dialog.Close(); }
            catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Export failed"); }
        }));
        var import = Button("Import library…", () => {
            var picker = new Microsoft.Win32.OpenFileDialog { Title = "Import Orbit library", Filter = "Orbit library|*.json" };
            if (picker.ShowDialog(dialog) != true) return;
            try
            {
                if (new FileInfo(picker.FileName).Length > 20 * 1024 * 1024) throw new InvalidDataException("Choose a library smaller than 20 MB.");
                var incoming = LibraryOperations.ParseExport(File.ReadAllText(picker.FileName));
                var choice = MessageBox.Show(dialog, $"Import {incoming.Links.Count} websites in {incoming.Collections.Count} collections?\n\nYes: Merge with your library (skip matching name, URL, and collection).\nNo: Replace your library.\nCancel: Make no changes.", "Choose import mode", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
                if (choice == MessageBoxResult.Cancel) return;
                var next = choice == MessageBoxResult.Yes ? LibraryOperations.Merge(entries, incoming.Links) : incoming.Links;
                var order = choice == MessageBoxResult.Yes ? collectionOrder.Concat(incoming.Collections).Distinct().ToList() : incoming.Collections;
                if (Save(next, order)) { deleted.Clear(); UpdateUndoNotice(); dialog.Close(); }
            }
            catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Import failed — library unchanged"); }
        }); import.Margin = new Thickness(0,12,0,18); panel.Children.Add(import);
        panel.Children.Add(Button("Cancel", dialog.Close)); dialog.ShowDialog();
    }
    bool DeleteShortcut(Shortcut item)
    {
        var index = entries.FindIndex(x => x.Id == item.Id); if (index < 0) return false;
        var next = entries.ToList(); next.RemoveAt(index);
        if (!Save(next)) return false;
        deleted.Push((item, index)); UpdateUndoNotice(); return true;
    }
    void UpdateUndoNotice()
    {
        undoNotice.Visibility = deleted.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        status.Text = deleted.TryPeek(out var latest) ? $"Removed {latest.Item.Name}" : "";
        if (undoButton != null) undoButton.IsEnabled = deleted.Count > 0;
    }
    void UndoDelete()
    {
        if (!deleted.TryPeek(out var restore)) return;
        var next = entries.ToList(); next.Insert(Math.Min(restore.Index, next.Count), restore.Item);
        if (Save(next)) { deleted.Pop(); UpdateUndoNotice(); }
    }
    void ManageCollections()
    {
        var dialog = Dialog("Collections", out var panel, 540);
        panel.Children.Add(new TextBlock { Text = "Select a collection to rename, open, or remove.\nDrag collections in the sidebar to reorder them.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,14) });
        var list = new ComboBox { ItemsSource = collectionOrder.ToList(), SelectedItem = selectedGroup != "All" ? selectedGroup : collectionOrder.FirstOrDefault(), MinHeight = 34, Foreground = Brushes.Black }; panel.Children.Add(list);
        var name = new TextBox { MaxLength = 60, Margin = new Thickness(0,14,0,12), Text = list.SelectedItem as string ?? "" }; panel.Children.Add(name);
        list.SelectionChanged += (_, _) => name.Text = list.SelectedItem as string ?? "";
        var result = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brush("#A83C38"), Margin = new Thickness(0,8,0,8) };
        var row = new StackPanel { Orientation = Orientation.Horizontal }; panel.Children.Add(row);
        row.Children.Add(Button("Rename", () => {
            if (list.SelectedItem is not string old) return;
            var value = name.Text.Trim();
            if (value.Length == 0 || value == "All") { result.Text = "Choose a collection name other than All."; return; }
            if (collectionOrder.Any(x => x != old && x.Equals(value, StringComparison.OrdinalIgnoreCase))) { result.Text = "That collection already exists."; return; }
            if (Save(entries.Select(x => x.Group == old ? x with { Group = value } : x).ToList(), collectionOrder.Select(x => x == old ? value : x).ToList())) { selectedGroup = value; Render(); dialog.Close(); }
        }));
        var create = Button("New collection", () => {
            var value = name.Text.Trim();
            if (value.Length == 0 || value == "All" || collectionOrder.Any(x => x.Equals(value, StringComparison.OrdinalIgnoreCase))) { result.Text = "Enter a new, unique name other than All."; return; }
            if (Save(entries, collectionOrder.Append(value).ToList())) { selectedGroup = value; Render(); dialog.Close(); }
        }); create.Margin = new Thickness(10,0,0,0); row.Children.Add(create);
        var open = Button("Open collection", () => { if (list.SelectedItem is string group) OpenCollection(group); }); open.Margin = new Thickness(0,12,0,10); panel.Children.Add(open);
        panel.Children.Add(Button("Delete collection", () => {
            if (list.SelectedItem is not string old) return;
            var destination = old == "Unsorted" ? "Everyday" : "Unsorted";
            if (MessageBox.Show(dialog, $"Delete {old}? Its websites will be kept in {destination}.", "Delete collection", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
            if (Save(entries.Select(x => x.Group == old ? x with { Group = destination } : x).ToList(), collectionOrder.Where(x => x != old).ToList())) dialog.Close();
        }, "#F8E9E5")); panel.Children.Add(result); panel.Children.Add(Button("Close", dialog.Close)); dialog.ShowDialog();
    }
    void OpenCollection(string group)
    {
        var links = entries.Where(x => x.Group == group).Select(x => x.Url).Distinct().ToList();
        if (links.Count == 0) { MessageBox.Show(this, "This collection has no websites yet."); return; }
        if (links.Count > 1 && MessageBox.Show(this, $"Open {links.Count} websites from {group} in your default browser?", "Open collection", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        var errors = new List<string>();
        foreach (var url in links) { try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch (Exception ex) { errors.Add(ex.Message); } }
        if (errors.Count > 0) MessageBox.Show(this, $"{errors.Count} websites could not be opened.\n" + errors[0], "Open collection");
    }
    static void DragSource(FrameworkElement source, string format, string value, Action<bool>? dragState = null)
    {
        Point? start = null;
        source.PreviewMouseLeftButtonDown += (_, e) => start = e.GetPosition(source);
        source.PreviewMouseLeftButtonUp += (_, _) => start = null;
        source.PreviewMouseMove += (_, e) => {
            if (e.LeftButton != MouseButtonState.Pressed || start is not Point origin) return;
            var current = e.GetPosition(source);
            if (Math.Abs(current.X - origin.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(current.Y - origin.Y) < SystemParameters.MinimumVerticalDragDistance) return;
            start = null; e.Handled = true;
            // Cancel a nested button's pressed state so dragging a card never opens its website.
            Mouse.Capture(null);
            dragState?.Invoke(true);
            try { DragDrop.DoDragDrop(source, new DataObject(format, value), DragDropEffects.Move); }
            finally { dragState?.Invoke(false); }
        };
    }
    void ConfigureCard(Border card, Shortcut item)
    {
        bool dragging = false, dropTarget = false;
        void UpdateOutline()
        {
            var highlighted = dragging || dropTarget || card.IsKeyboardFocusWithin;
            card.BorderBrush = highlighted ? Brush("#56B879") : Brush(card.IsMouseOver ? "#ACBDA4" : "#E5E8E0");
            card.BorderThickness = new Thickness(highlighted ? 2 : 1);
            card.Padding = highlighted ? new Thickness(19,17,19,13) : new Thickness(20,18,20,14);
            card.Opacity = dragging ? 0.65 : 1;
        }
        card.MouseEnter += (_, _) => UpdateOutline(); card.MouseLeave += (_, _) => UpdateOutline();
        card.IsKeyboardFocusWithinChanged += (_, _) => UpdateOutline();
        DragSource(card, "Orbit.Website", item.Id, active => { dragging = active; UpdateOutline(); }); card.AllowDrop = true;
        card.DragEnter += (_, e) => { dropTarget = e.Data.GetDataPresent("Orbit.Website"); UpdateOutline(); };
        card.DragLeave += (_, _) => { dropTarget = false; UpdateOutline(); };
        card.DragOver += (_, e) => { e.Effects = e.Data.GetDataPresent("Orbit.Website") ? DragDropEffects.Move : DragDropEffects.None; e.Handled = true; };
        card.Drop += (_, e) => {
            dropTarget = false; UpdateOutline();
            if (e.Data.GetData("Orbit.Website") is string id && entries.FirstOrDefault(x => x.Id == id) is Shortcut moving)
                Save(e.GetPosition(card).Y > card.ActualHeight / 2 ? LibraryOperations.MoveAfter(entries, moving, item) : LibraryOperations.MoveBefore(entries, moving, item));
            e.Handled = true;
        };
        var menu = new ContextMenu();
        foreach (var label in new[] { "Move earlier", "Move later" })
        {
            var option = new MenuItem { Header = label }; option.Click += (_, _) => {
                var index = entries.FindIndex(x => x.Id == item.Id); var destination = index + (label == "Move earlier" ? -1 : 1);
                if (index < 0 || destination < 0 || destination >= entries.Count) return;
                var next = entries.ToList(); (next[index], next[destination]) = (next[destination], next[index]); Save(next);
            }; menu.Items.Add(option);
        }
        card.ContextMenu = menu;
    }
    void ConfigureCollection(Button button, string group)
    {
        button.ToolTip = "Drag to reorder • Right-click for collection actions";
        DragSource(button, "Orbit.Collection", group); button.AllowDrop = true;
        button.DragOver += (_, e) => { e.Effects = e.Data.GetDataPresent("Orbit.Collection") ? DragDropEffects.Move : DragDropEffects.None; e.Handled = true; };
        button.Drop += (_, e) => {
            if (e.Data.GetData("Orbit.Collection") is string moving)
                Save(entries, e.GetPosition(button).Y > button.ActualHeight / 2 ? LibraryOperations.MoveAfter(collectionOrder, moving, group) : LibraryOperations.MoveBefore(collectionOrder, moving, group));
            e.Handled = true;
        };
        var menu = new ContextMenu(); var open = new MenuItem { Header = "Open collection" }; open.Click += (_, _) => OpenCollection(group); menu.Items.Add(open);
        var manage = new MenuItem { Header = "Manage collection" }; manage.Click += (_, _) => { selectedGroup = group; ManageCollections(); }; menu.Items.Add(manage);
        foreach (var label in new[] { "Move earlier", "Move later" })
        {
            var option = new MenuItem { Header = label }; option.Click += (_, _) => {
                var index = collectionOrder.IndexOf(group); var destination = index + (label == "Move earlier" ? -1 : 1);
                if (index < 0 || destination < 0 || destination >= collectionOrder.Count) return;
                var next = collectionOrder.ToList(); (next[index], next[destination]) = (next[destination], next[index]); Save(entries, next);
            }; menu.Items.Add(option);
        }
        button.ContextMenu = menu;
    }
}
