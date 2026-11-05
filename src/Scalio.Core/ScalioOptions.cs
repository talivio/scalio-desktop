namespace Scalio.Core;

/// <summary>
/// Ürün ayarları.
///
/// ⚠️ Buradaki değerlerin hiçbiri SIR DEĞİLDİR ve olmamalıdır: kaynak kodu
/// açık. Uygulama hesap, anahtar ya da jeton taşımaz; yalnız herkese açık
/// sürüm ucunu sorar.
/// </summary>
public static class ScalioOptions
{
    /// <summary>scalio.talivio.com — sürüm denetimi (`/api/version`) buradan sorulur.</summary>
    public static string RelayUrl => Environment.GetEnvironmentVariable("SCALIO_RELAY_URL")
                                     ?? "https://scalio.talivio.com";

    /// <summary>
    /// `realesrgan-ncnn-vulkan.exe` ve `models/` klasörünün bulunduğu yer.
    ///
    /// ⚠️ Motor projeye kopyalanmaz — kurulumda uygulamanın yanındaki
    /// `engine/` klasörüne konur. Varsayılan bu şekilde hesaplanır; geliştirme
    /// ve testlerde ortam değişkeniyle gerçek motor kopyasının yolu verilebilir
    /// (`ScalioPaths`'teki "ortam değişkeniyle veri kökü yönlendirilmez"
    /// ilkesi burada geçerli DEĞİL — bu bir yetki sınırı değil, yalnızca
    /// çalışma zamanında hangi ikilinin kullanılacağı; motor dosyaları zaten
    /// imzasız/harici bir bağımlılık).
    /// </summary>
    public static string EnginePath => Environment.GetEnvironmentVariable("SCALIO_ENGINE_PATH")
                                       ?? Path.Combine(AppContext.BaseDirectory, "engine");

    /// <summary>Motor ikilisinin adı: Windows'ta `.exe` uzantılı, macOS ve Linux'ta uzantısız.</summary>
    public static string EngineFileName => OperatingSystem.IsWindows()
        ? "realesrgan-ncnn-vulkan.exe"
        : "realesrgan-ncnn-vulkan";

    /// <summary>
    /// Motor ikilisinin tam yolu.
    /// </summary>
    /// <remarks>
    /// ⚠️ macOS paketinde (`Scalio.app`) motor `engine/` altında DEĞİL,
    /// `Contents/MacOS/` içindedir: Apple imzası çalıştırılabilir kodu orada
    /// bekler, modeller (veri) ise `Contents/Resources/`'a gider. Karışık bir
    /// klasör imzalama ve noter onayında sorun çıkarır. Uygulamanın kendisi
    /// mimariye göre bir alt klasörde (`Contents/MacOS/arm64/`) çalışır;
    /// universal motor iki mimariye ortaktır (bkz. tools/package-macos.sh).
    /// </remarks>
    public static string EngineExecutable => MacBundleContents() is { } contents
        ? Path.Combine(contents, "MacOS", EngineFileName)
        : Path.Combine(EnginePath, EngineFileName);

    /// <summary>Model dosyalarının (`*.param`/`*.bin`) klasörü.</summary>
    public static string ModelsPath => MacBundleContents() is { } contents
        ? Path.Combine(contents, "Resources", "engine", "models")
        : Path.Combine(EnginePath, "models");

    /// <summary>
    /// Bir macOS `.app` paketinin içinden çalışılıyorsa paketin `Contents`
    /// klasörü; değilse (ya da geliştirmede motor yolu verildiyse) null.
    /// </summary>
    private static string? MacBundleContents()
    {
        if (!OperatingSystem.IsMacOS() || Environment.GetEnvironmentVariable("SCALIO_ENGINE_PATH") is not null)
        {
            return null;
        }

        const string marker = "/Contents/MacOS/";

        var index = AppContext.BaseDirectory.LastIndexOf(marker, StringComparison.Ordinal);

        return index < 0 ? null : AppContext.BaseDirectory[..(index + "/Contents".Length)];
    }
}
