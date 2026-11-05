namespace Scalio.Shell;

/// <summary>
/// Köprünün işletim sistemine bıraktığı işler: dosya/klasör seçici.
///
/// ⚠️ Yalnız GERÇEKTEN farklı olan şeyler burada. Tarayıcıda adres açmak ve
/// klasör açmak .NET'in `UseShellExecute`'u ile üç sistemde de aynı çalışıyor
/// (Windows: kabuk, macOS: `open`, Linux: `xdg-open`); onları arayüzün
/// arkasına almak yalnız soyutlama kalabalığı olurdu.
/// </summary>
public interface IDesktopPlatform
{
    /// <summary>Çoklu görsel seçimi; vazgeçilirse boş dizi.</summary>
    string[] PickFiles(string title, IReadOnlyList<string> extensions);

    /// <summary>Tek klasör seçimi; vazgeçilirse null.</summary>
    string? PickFolder(string title);
}
