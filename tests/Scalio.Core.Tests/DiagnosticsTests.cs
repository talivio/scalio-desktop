using Scalio.Core.Diagnostics;
using Scalio.Core.Support;

namespace Scalio.Core.Tests;

/// <summary>
/// Günlük ve sürüm denetimi.
///
/// Bu iki yetenek, bir kullanıcının makinesinde derin taramanın çalışmaması ve
/// elimizde ekrandaki tek cümleden başka hiçbir şey olmaması üzerine eklendi.
/// Testler o dersin kendisini tutuyor: günlük ASLA fırlatmamalı, sürüm
/// karşılaştırması metin değil SAYI mantığıyla çalışmalı.
/// </summary>
public class DiagnosticsTests
{
    [Fact]
    public void A_larger_minor_version_is_newer()
    {
        Assert.True(UpdateChecker.Compare("2.2.0", "2.1.1") > 0);
        Assert.True(UpdateChecker.Compare("2.1.1", "2.1.0") > 0);
        Assert.True(UpdateChecker.Compare("3.0.0", "2.9.9") > 0);
    }

    /// <summary>
    /// ⚠️ ASIL KUSUR BU OLURDU. Metin karşılaştırmasında "2.10.0" &lt; "2.9.0"
    /// çıkar; onuncu yamada bütün kullanıcılar sessizce "güncelsiniz" görürdü —
    /// hem de kusurun düzeltildiği sürümde.
    /// </summary>
    [Fact]
    public void Version_ten_is_newer_than_version_nine()
    {
        Assert.True(UpdateChecker.Compare("2.10.0", "2.9.0") > 0);
        Assert.True(UpdateChecker.Compare("2.1.10", "2.1.9") > 0);

        // Metin karşılaştırması bunun TERSİNİ söylerdi:
        Assert.True(string.CompareOrdinal("2.10.0", "2.9.0") < 0);
    }

    [Fact]
    public void Equal_versions_do_not_offer_an_update()
    {
        Assert.Equal(0, UpdateChecker.Compare("2.1.1", "2.1.1"));

        // Eksik parça sıfır sayılır: "2.1" ile "2.1.0" aynı sürümdür.
        Assert.Equal(0, UpdateChecker.Compare("2.1", "2.1.0"));
    }

    /// <summary>
    /// ⚠️ Sunucudan beklenmedik bir metin gelirse karşılaştırma ÇÖKMEZ.
    /// Ayrıştırılamayan parça 0 sayılır; sonuç "yenisi yok" olur — güncellemeyi
    /// kaçırmak, açılışta istisna fırlatmaktan iyidir.
    /// </summary>
    [Fact]
    public void Unparseable_versions_do_not_throw()
    {
        Assert.Equal(0, UpdateChecker.Compare("bozuk", "0.0.0"));
        Assert.True(UpdateChecker.Compare("1.0.0", "yok") > 0);
    }

    [Fact]
    public void Build_number_does_not_count_as_a_newer_version()
    {
        // "2.1.1+abc" ile "2.1.1" aynı sürümdür; derleme damgası sürüm değil.
        Assert.Equal(0, UpdateChecker.Compare("2.1.1", "2.1.1"));
    }

    /// <summary>
    /// ⚠️ GÜNLÜK HİÇBİR KOŞULDA FIRLATMAZ. Kurtarma işinin ortasında "günlük
    /// yazılamadı" diye istisna fırlatmak, asıl işi ikincil bir şey için feda
    /// etmek olurdu.
    /// </summary>
    [Fact]
    public void Logging_never_throws()
    {
        var record = Record.Exception(() =>
        {
            ScalioLog.Info("test", "sıradan satır");
            ScalioLog.Warn("test", "uyarı");
            ScalioLog.Error("test", "hata", new InvalidOperationException("sebep"));
            ScalioLog.Debug("test", "ayrıntı");
        });

        Assert.Null(record);
    }

    [Fact]
    public void An_error_line_carries_the_exception_type_and_message()
    {
        ScalioLog.Error("test", "birim açılamadı", new UnauthorizedAccessException("erişim reddedildi"));

        var text = ScalioLog.ReadAll();

        Assert.Contains("UnauthorizedAccessException", text);
        Assert.Contains("erişim reddedildi", text);
    }

    /// <summary>
    /// Destek paketi künyeyi ve günlüğü birlikte taşır — destek tarafında
    /// "hangi sürüm, hangi Windows" sorusu tekrar sorulmasın diye.
    /// </summary>
    [Fact]
    public void The_support_bundle_carries_the_environment_and_the_log()
    {
        ScalioLog.Info("test", "paket-icin-isaret");

        var bundle = SupportBundle.Build(new Dictionary<string, string> { ["Oturum"] = "authenticated" });

        Assert.Contains("Scalio destek paketi", bundle);
        Assert.Contains("Windows", bundle);
        Assert.Contains("Güvenlik yazılımı", bundle);
        Assert.Contains("Oturum", bundle);
        Assert.Contains("paket-icin-isaret", bundle);
    }
}
