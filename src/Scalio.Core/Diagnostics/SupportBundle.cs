using System.Text;
using Scalio.Core.Localization;

namespace Scalio.Core.Diagnostics;

/// <summary>
/// Destek için tek bir metin dosyası: künye + günlükler.
///
/// ⚠️ NEDEN "PAKET" DEĞİL DÜZ METİN. Zip üretmek cazipti ama kullanıcı ne
/// gönderdiğini GÖREMEZDİ. Bu bir kurtarma aracı: günlüklerde birim harfleri
/// ve hata kodları geçiyor, kullanıcının "bunu gönderiyorum" diyebilmesi
/// gerekiyor. Açılıp okunabilen tek dosya, hem güveni hem de destek tarafında
/// açma zahmetini çözüyor.
///
/// ⚠️ DOSYA ADLARI GÜNLÜĞE HİÇ YAZILMIYOR (bkz. ScalioLog kullanım yerleri),
/// bu yüzden pakette de yoklar. Kurtarılan dosyaların adları kullanıcının en
/// hassas verisi; teşhis için gereken şey hangi ADIMDA kırıldığı, hangi
/// dosyanın arandığı değil.
/// </summary>
public static class SupportBundle
{
    /// <summary>Paketi üretir ve yazdığı dosyanın yolunu döndürür.</summary>
    /// <param name="extra">Köprünün eklediği durum satırları (oturum, abonelik…).</param>
    public static string Write(IReadOnlyDictionary<string, string>? extra = null)
    {
        var path = Path.Combine(
            ScalioLog.Directory,
            $"scalio-destek-{DateTime.Now:yyyyMMdd-HHmmss}.txt");

        File.WriteAllText(path, Build(extra), Encoding.UTF8);

        return path;
    }

    /// <summary>Paketin içeriğini üretir (dosyaya yazmadan).</summary>
    public static string Build(IReadOnlyDictionary<string, string>? extra = null)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Scalio destek paketi")
            .AppendLine("════════════════════════════════════════════")
            .AppendLine($"Üretildi   : {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}")
            .AppendLine($"Sürüm      : {Version()}")
            .AppendLine($"Windows    : {Environment.OSVersion.VersionString}")
            .AppendLine($"Mimari     : {System.Runtime.InteropServices.RuntimeInformation.OSArchitecture}")
            .AppendLine($".NET       : {Environment.Version}")
            .AppendLine($"Dil        : {Strings.Language}")
            .AppendLine($"Taşınabilir: {(ScalioPaths.IsPortable ? "evet" : "hayır")}")
            .AppendLine($"Veri kökü  : {ScalioPaths.DataRoot}")
            .AppendLine($"Ayrıntı    : {(ScalioLog.VerboseEnabled ? "açık" : "kapalı")}");

        if (extra is not null)
        {
            foreach (var (key, value) in extra)
            {
                builder.AppendLine($"{key,-11}: {value}");
            }
        }

        /*
         * ⚠️ ANTİVİRÜS LİSTESİ PAKETE GİRİYOR ve bu, en çok işe yarayan
         * satırlardan biri. Yükseltilmiş yardımcı sürecin öldürüldüğü olayda
         * ilk sorulan soru "hangi antivirüs" oluyor; kullanıcıya sormak yerine
         * ölçmek, hem hızlı hem de yanlış cevaba kapalı.
         */
        builder.AppendLine().AppendLine("Güvenlik yazılımı").AppendLine("────────────────────────");
        builder.AppendLine(SecurityProducts());

        builder.AppendLine().AppendLine("Günlük").AppendLine("────────────────────────");
        builder.Append(ScalioLog.ReadAll());

        return builder.ToString();
    }

    private static string Version()
    {
        try
        {
            return ScalioVersion.Current;
        }
        catch
        {
            return "?";
        }
    }

    /// <summary>
    /// Kurulu antivirüs/güvenlik duvarı ürünleri (WMI, Güvenlik Merkezi).
    /// </summary>
    /// <remarks>
    /// ⚠️ SORGU BAŞARISIZ OLABİLİR VE BU NORMAL: Windows Server'da
    /// `SecurityCenter2` ad alanı yoktur. Bunu bir arıza gibi göstermek,
    /// paketin geri kalanını okunmaz hâle getirirdi — tek satırlık bir not
    /// düşülüp devam edilir.
    /// </remarks>
    private static string SecurityProducts()
    {
        if (!OperatingSystem.IsWindows())
        {
            return "(yalnız Windows)";
        }

        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                @"root\SecurityCenter2",
                "SELECT displayName, productState FROM AntiVirusProduct");

            var found = new List<string>();

            foreach (var item in searcher.Get())
            {
                var name = item["displayName"]?.ToString() ?? "?";
                var state = item["productState"];

                /*
                 * `productState` bir bit alanı; ikinci baytın 0x10'u "gerçek
                 * zamanlı koruma açık" demek. Kullanıcının "korumayı kapattım"
                 * dediği ama alt bileşenlerin açık kaldığı durumu ayırt eden
                 * tek ölçüm bu.
                 */
                var realtime = state is not null && ((Convert.ToInt32(state) >> 8) & 0x10) != 0;

                found.Add($"  {name} · gerçek zamanlı koruma: {(realtime ? "AÇIK" : "kapalı")}");
            }

            return found.Count > 0 ? string.Join('\n', found) : "  (kayıtlı ürün yok)";
        }
        catch (Exception e)
        {
            return $"  (okunamadı: {e.Message})";
        }
    }
}
