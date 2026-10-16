using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using Orbit;

internal static class ThemeChecks
{
    [STAThread]
    static void Main()
    {
        var discovered = WebsiteIcons.Discover("<base href='/assets/'><link href='logo.png?a=1&amp;b=2' rel='apple-touch-icon'><link rel='shortcut icon' href=../favicon.ico><link rel='icon' href='icon.svg'><link rel='icon' href='javascript:alert(1)'>", new Uri("https://example.com/page")).ToArray();
        if (discovered.Length != 2 || discovered[0].AbsoluteUri != "https://example.com/assets/logo.png?a=1&b=2" || discovered[1].AbsoluteUri != "https://example.com/favicon.ico") throw new Exception("Icon discovery failed");
        Console.WriteLine("PASS icon discovery resolves relative URLs, entities, and filters unsupported icons");
        foreach (var site in new[] { "https://github.com", "https://www.google.com" })
            Console.WriteLine("Live icon " + site + ": " + (WebsiteIcons.Fetch(site).GetAwaiter().GetResult() != null ? "loaded" : "fallback"));
        var app = new App(); app.InitializeComponent();
        SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext());
        var isolated = Path.Combine(Path.GetTempPath(), "OrbitPreview-" + Guid.NewGuid().ToString("N"));
        var window = new MainWindow(new SettingsStore(@"Software\OrbitPreview-" + Guid.NewGuid().ToString("N"), Path.Combine(isolated, "absent.json"), isolated));
        var content = (FrameworkElement)window.Content;
        window.WindowState = WindowState.Normal;
        content.Width = 1100; content.Height = 680;
        // Let asynchronous website icons finish without reading or altering the user's library.
        var wait = new System.Windows.Threading.DispatcherFrame();
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        timer.Tick += (_, _) => { timer.Stop(); wait.Continue = false; };
        timer.Start(); System.Windows.Threading.Dispatcher.PushFrame(wait);
        var output = Path.Combine(Path.GetTempPath(), "OrbitThemePreview"); Directory.CreateDirectory(output);
        foreach (var dark in new[] { false, true, false })
        {
            Theme.Apply(dark);
            SetSwitch(content, dark);
            content.Measure(new Size(1100, 680)); content.Arrange(new Rect(0, 0, 1100, 680)); content.UpdateLayout();
            if (((SolidColorBrush)window.Background).Color != (Color)ColorConverter.ConvertFromString(dark ? "#202020" : "#FAF9F6")) throw new Exception("Window theme did not update");
            var bitmap = new RenderTargetBitmap(1100, 680, 96, 96, PixelFormats.Pbgra32);
            var frame = new DrawingVisual(); using (var dc = frame.RenderOpen()) { dc.DrawRectangle(window.Background, null, new Rect(0,0,1100,680)); dc.DrawRectangle(new VisualBrush(content) { ViewboxUnits = BrushMappingMode.Absolute, Viewbox = new Rect(0,0,1100,680) }, null, new Rect(0,0,1100,680)); }
            bitmap.Render(frame); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = File.Create(Path.Combine(output, dark ? "dark.png" : "light.png")); encoder.Save(file);
            Console.WriteLine("PASS rendered " + (dark ? "dark" : "light") + " theme");
        }
        Console.WriteLine(output);
        window.Close(); app.Shutdown();
    }
    static void SetSwitch(DependencyObject parent, bool dark)
    {
        if (parent is CheckBox toggle && toggle.Content as string == "Dark mode") toggle.IsChecked = dark;
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++) SetSwitch(VisualTreeHelper.GetChild(parent, index), dark);
    }
}
