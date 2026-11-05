using System.Reflection;

namespace Scalio.Shell;

/// <summary>
/// Gömülü arayüz dosyalarının iki kabuk için ortak sunumu.
/// </summary>
public static class UiResources
{
    /// <summary>Arayüzün gömülü olduğu derleme (kendini sınama da buna bakar).</summary>
    public static Assembly Assembly => typeof(UiResources).Assembly;

    /// <summary>
    /// İstenen adresin gömülü karşılığı; yoksa null.
    /// </summary>
    /// <remarks>
    /// ⚠️ Yol DOĞRUDAN kaynak adına çevrilmez: `..` ya da eğik çizgi
    /// oyunlarıyla başka bir gömülü kaynağa ulaşmayı denemek anlamsız olsun
    /// diye yalnızca dosya adı alınıyor. Arayüz düz bir klasör, alt dizini yok.
    /// </remarks>
    public static (Stream Stream, string ContentType)? Open(Uri uri)
    {
        var path = uri.AbsolutePath.TrimStart('/');

        if (path.Length == 0)
        {
            path = "index.html";
        }

        var stream = Assembly.GetManifestResourceStream("ui/" + Path.GetFileName(path));

        return stream is null ? null : (stream, ContentType(path));
    }

    private static string ContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".html" => "text/html; charset=utf-8",
        ".css" => "text/css; charset=utf-8",
        ".js" => "text/javascript; charset=utf-8",
        ".svg" => "image/svg+xml",
        ".png" => "image/png",
        _ => "application/octet-stream",
    };
}
