using Photino.NET;
using Scalio.Shell;

namespace Scalio.Desktop;

/// <summary>macOS (NSOpenPanel) ve Linux (GTK) dosya/klasör seçicileri, Photino üzerinden.</summary>
public sealed class PhotinoDesktopPlatform(PhotinoWindow window) : IDesktopPlatform
{
    public string[] PickFiles(string title, IReadOnlyList<string> extensions)
    {
        (string Name, string[] Extensions)[] filters =
        [
            ("Images", extensions.Select(e => "*" + e).ToArray()),
        ];

        return window.ShowOpenFile(title, DefaultFolder(), multiSelect: true, filters) ?? [];
    }

    public string? PickFolder(string title)
        => window.ShowOpenFolder(title, DefaultFolder(), multiSelect: false)?.FirstOrDefault();

    /// <summary>Seçici Resimler klasöründe açılır; o yoksa ev dizininde.</summary>
    private static string DefaultFolder()
    {
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        return Directory.Exists(pictures) ? pictures : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }
}
