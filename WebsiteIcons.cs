using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace ShortcutsPanel;

public static class WebsiteIcons
{
    static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(12) };
    static readonly SemaphoreSlim Slots = new(4);
    static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(200);

    // Read declared favicons and touch icons, including relative and CDN URLs.
    public static IEnumerable<Uri> Discover(string html, Uri page)
    {
        var baseTag = Regex.Match(html, @"<base\b[^>]*>", RegexOptions.IgnoreCase, RegexTimeout);
        var baseAttrs = Attributes(baseTag.Value);
        if (baseAttrs.TryGetValue("href", out var baseHref) && SafeUri(page, baseHref) is Uri resolvedBase) page = resolvedBase;
        var found = new List<(Uri Uri, int Rank)>();
        foreach (Match tag in Regex.Matches(html, @"<link\b[^>]*>", RegexOptions.IgnoreCase, RegexTimeout))
        {
            var attrs = Attributes(tag.Value);
            if (!attrs.TryGetValue("rel", out var rel) || !attrs.TryGetValue("href", out var href)) continue;
            var tokens = rel.ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (!tokens.Contains("icon") && !tokens.Contains("apple-touch-icon") && !tokens.Contains("apple-touch-icon-precomposed")) continue;
            var uri = SafeUri(page, href);
            if (uri == null || uri.AbsolutePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) || attrs.GetValueOrDefault("type", "").Contains("svg", StringComparison.OrdinalIgnoreCase)) continue;
            found.Add((uri, tokens.Contains("apple-touch-icon") ? 0 : 1));
        }
        return found.OrderBy(x => x.Rank).Select(x => x.Uri).Distinct().Take(6).ToArray();
    }
    static Dictionary<string, string> Attributes(string tag)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Regex.Matches(tag, "([\\w:-]+)\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)'|([^\\s>]+))", RegexOptions.None, RegexTimeout))
            result[match.Groups[1].Value] = WebUtility.HtmlDecode(match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Success ? match.Groups[3].Value : match.Groups[4].Value);
        return result;
    }
    static Uri? SafeUri(Uri page, string href) => Uri.TryCreate(page, href, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https") && string.IsNullOrEmpty(uri.UserInfo) ? uri : null;
    static async Task<(byte[] Bytes, Uri FinalUri)> Download(Uri uri, int limit, CancellationToken token)
    {
        using var response = await Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > limit) throw new IOException("Icon response too large.");
        using var stream = await response.Content.ReadAsStreamAsync(token);
        using var output = new MemoryStream(); var buffer = new byte[8192]; int count;
        while ((count = await stream.ReadAsync(buffer, token)) > 0)
        {
            if (output.Length + count > limit) throw new IOException("Icon response too large.");
            output.Write(buffer, 0, count);
        }
        return (output.ToArray(), response.RequestMessage?.RequestUri ?? uri);
    }
    public static async Task<BitmapSource?> Fetch(string address)
    {
        await Slots.WaitAsync();
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var page = new Uri(address); var candidates = new List<Uri>();
            try
            {
                using var pageTimeout = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
                pageTimeout.CancelAfter(TimeSpan.FromSeconds(5));
                var document = await Download(page, 1024 * 1024, pageTimeout.Token);
                page = document.FinalUri;
                candidates.AddRange(Discover(Encoding.UTF8.GetString(document.Bytes), page));
            }
            catch { /* Try the conventional favicon when HTML discovery is unavailable. */ }
            candidates.Add(new Uri(page.GetLeftPart(UriPartial.Authority) + "/favicon.ico"));
            foreach (var candidate in candidates.Distinct())
            {
                if (timeout.IsCancellationRequested) break;
                try
                {
                    using var iconTimeout = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
                    iconTimeout.CancelAfter(TimeSpan.FromSeconds(3));
                    var data = await Download(candidate, 512 * 1024, iconTimeout.Token);
                    using var bytes = new MemoryStream(data.Bytes);
                    var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = 32; bitmap.StreamSource = bytes; bitmap.EndInit(); bitmap.Freeze(); return bitmap;
                }
                catch { /* A missing or unsupported icon must not prevent using a shortcut. */ }
            }
            return null;
        }
        catch { return null; }
        finally { Slots.Release(); }
    }
}
