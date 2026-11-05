using Scalio.Shell;

namespace Scalio.App;

/// <summary>Windows dosya/klasör seçicileri (WPF).</summary>
public sealed class WpfDesktopPlatform : IDesktopPlatform
{
    public string[] PickFiles(string title, IReadOnlyList<string> extensions)
    {
        var patterns = string.Join(';', extensions.Select(e => "*" + e));

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            Multiselect = true,
            Filter = $"Görseller|{patterns}|Tüm dosyalar|*.*",
        };

        return dialog.ShowDialog() == true ? dialog.FileNames : [];
    }

    public string? PickFolder(string title)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = title };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
