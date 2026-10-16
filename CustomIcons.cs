using System.IO;
using System.Windows.Media.Imaging;

namespace Orbit;

public static class CustomIcons
{
    public static string FromFile(string path)
    {
        if (new FileInfo(path).Length > 5 * 1024 * 1024) throw new InvalidDataException("Choose an image smaller than 5 MB.");
        using var stream = File.OpenRead(path);
        var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.DecodePixelWidth = 96; bitmap.StreamSource = stream; bitmap.EndInit(); bitmap.Freeze();
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap)); using var output = new MemoryStream(); png.Save(output);
        var encoded = Convert.ToBase64String(output.ToArray()); Decode(encoded); return encoded;
    }
    public static BitmapSource Decode(string encoded)
    {
        if (encoded.Length > 350000) throw new InvalidDataException("Custom icon is too large.");
        using var stream = new MemoryStream(Convert.FromBase64String(encoded));
        var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.DecodePixelWidth = 96; bitmap.StreamSource = stream; bitmap.EndInit(); bitmap.Freeze();
        if (bitmap.PixelHeight > 1024) throw new InvalidDataException("Custom icon has unsupported dimensions.");
        return bitmap;
    }
}
