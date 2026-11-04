using Scalio.Core.Localization;

namespace Scalio.Core.Tests.Support;

/// <summary>
/// Dili geçici olarak değiştirir ve HER DURUMDA geri alır.
///
/// ⚠️ Elle geri almak yetmiyor: iddia (assert) patladığında geri alma satırına
/// hiç ulaşılmaz ve dil "tr" kalır. Aynı koleksiyondaki sonraki testler o
/// zaman İngilizce metin bekleyip Türkçe bulur — asıl kusur bir yerdeyken
/// takım bambaşka bir yeri suçlar.
/// </summary>
public sealed class LanguageScope : IDisposable
{
    private readonly string _previous = Strings.Language;

    public LanguageScope(string language) => Strings.Language = language;

    public void Dispose() => Strings.Language = _previous;
}

/// <summary>
/// Dili değiştiren testlerin ortak koleksiyonu.
///
/// ⚠️ `Strings.Language` GLOBAL bir ayar ve xUnit test SINIFLARINI varsayılan
/// olarak PARALEL çalıştırır. İki sınıf aynı anda dili değiştirdiğinde takım
/// rastgele kırmızıya dönüyordu: biri Türkçe metin beklerken diğeri dili
/// İngilizceye çekiyordu. Ölçüldü — art arda üç koşudan biri düşüyordu.
/// Aynı koleksiyondaki sınıflar ardışık çalışır.
/// </summary>
[CollectionDefinition(Name)]
public sealed class LanguageCollection
{
    public const string Name = "dil";
}
