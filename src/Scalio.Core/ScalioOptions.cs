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
}
