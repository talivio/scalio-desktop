namespace Scalio.Core.Engine;

/// <summary>
/// Görsel boyutuna göre başlangıç tile boyutu önerir.
///
/// ⚠️ BASİT TUTULDU — GPU bellek miktarını sorgulamıyoruz (Vulkan API'de bunu
/// güvenilir biçimde almak platforma/sürücüye göre değişir ve motor zaten
/// `-t 0` ile "otomatik" tile seçebiliyor). Yalnızca ÇOK büyük görsellerde
/// motorun otomatik seçimine güvenmek yerine daha küçük bir başlangıç
/// değeriyle başlamak, ilk denemede OOM'a düşme olasılığını azaltır.
/// `EsrganRunner` zaten OOM'da tile'ı daha da küçültüp yeniden dener; bu sınıf
/// yalnızca "iyi bir ilk tahmin" verir.
/// </summary>
public static class TileSizer
{
    /// <summary>Bu kenar uzunluğunun üzerinde otomatik yerine sabit bir tile önerilir.</summary>
    public const int LargeEdgeThreshold = 4000;

    /// <summary>Bu kenar uzunluğunun üzerinde daha da küçük bir tile önerilir.</summary>
    public const int VeryLargeEdgeThreshold = 8000;

    /// <summary>
    /// Önerilen tile boyutu. `null` dönerse motorun kendi otomatik seçimi
    /// (`-t` verilmez) kullanılmalıdır.
    /// </summary>
    public static int? Suggest(int width, int height)
    {
        var longestEdge = Math.Max(width, height);

        if (longestEdge > VeryLargeEdgeThreshold)
        {
            return 200;
        }

        if (longestEdge > LargeEdgeThreshold)
        {
            return 400;
        }

        return null;
    }
}
