using System.Windows;

using System.Windows.Media;

using System.Windows.Data;

using System.ComponentModel;



namespace ShortcutsPanel;



public static class Theme

{

    static readonly Dictionary<string, string> DarkColors = new(StringComparer.OrdinalIgnoreCase)

    {

        ["#FAF9F6"] = "#202020", ["#252924"] = "#EEEEEE",

        ["#F1F2ED"] = "#252525", ["#E2E5DD"] = "#3B3B3B",

        ["#294B3D"] = "#505050", ["#294B3C"] = "#E0E0E0", ["#80877D"] = "#AAAAAA",

        ["#8C9388"] = "#AAAAAA", ["#838B7F"] = "#AAAAAA",

        ["#7B8377"] = "#BBBBBB", ["#868D80"] = "#AAAAAA",

        ["#DFE6DA"] = "#3D3D3D", ["#374632"] = "#E5E5E5",

        ["#FFFFFF"] = "#2B2B2B", ["#A2AA9B"] = "#AAAAAA",

        ["#F5F6F2"] = "#EAEAEA", ["#252D23"] = "#EEEEEE",

        ["#8B9285"] = "#AAAAAA", ["#E5E8E0"] = "#414141",

        ["#ACBDA4"] = "#777777", ["#EEF0E9"] = "#3A3A3A",

        ["#DDE2D6"] = "#555555", ["#A83C38"] = "#FFB3AB",

        ["#F8E9E5"] = "#583936", ["#53654B"] = "#505050",

        ["#738B65"] = "#999999", ["#528462"] = "#777777",

        ["#AACB94"] = "#BBBBBB", ["#8A9485"] = "#666666"

    };

    static readonly Dictionary<string, SolidColorBrush> Brushes = new(StringComparer.OrdinalIgnoreCase);

    static readonly Dictionary<string, ThemeColor> Colors = new(StringComparer.OrdinalIgnoreCase);

    sealed class ThemeColor : INotifyPropertyChanged

    {

        public Color Color { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Set(Color color) { Color = color; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color))); }

    }

    public static bool IsDark { get; private set; }

    public static SolidColorBrush Brush(string light)

    {

        if (!Brushes.TryGetValue(light, out var brush))

        {

            var value = new ThemeColor(); value.Set(ColorFor(light)); Colors[light] = value;

            Brushes[light] = brush = new SolidColorBrush();

            // A binding keeps WPF styles from freezing a brush that must change at runtime.

            BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, new Binding(nameof(ThemeColor.Color)) { Source = value });

        }

        return brush;

    }

    static Color ColorFor(string light) => (Color)ColorConverter.ConvertFromString(

        IsDark && DarkColors.TryGetValue(light, out var dark) ? dark : light);

    public static void Apply(bool dark)

    {

        IsDark = dark;

        foreach (var pair in Colors) pair.Value.Set(ColorFor(pair.Key));

        var resources = Application.Current.Resources;

        resources["WindowBackground"] = Brush("#FAF9F6");

        resources["Ink"] = Brush("#252924");

        resources["ControlBackground"] = Brush("#EEF0E9");

        resources["InputBackground"] = Brush("#FFFFFF");

        resources["InputBorder"] = Brush("#DDE2D6");

        resources["FocusBorder"] = Brush("#738B65");

        resources["SwitchOn"] = Brush("#528462");

        resources["SwitchOff"] = Brush("#8A9485");

        resources["SwitchFocus"] = Brush("#AACB94");

    }

}
