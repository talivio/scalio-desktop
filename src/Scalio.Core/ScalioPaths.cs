namespace Scalio.Core;

/// <summary>
/// Uygulamanın kendi verisini NEREYE yazdığı.
///
/// ⚠️ NEDEN AYRI BİR SINIF: bir denetimde şu ortaya çıktı — "kaynak diske
/// yazmıyoruz" ilkesi yalnızca KURTARILAN DOSYALAR için doğruydu. Uygulamanın
/// kendisi (WebView2 profili, oturum kaydı, abonelik önbelleği, iş dosyaları)
/// `%LOCALAPPDATA%`ya, yani genelde C:'ye yazıyordu. Tek diskli bir makinede bu,
/// kurtarma daha başlamadan kümelerin üzerine yazmak demek.
///
/// ⚠️ TAŞINABİLİR MOD İŞARET DOSYASIYLA AÇILIR: uygulamanın yanında
/// `scalio.portable` varsa tüm veri `&lt;uygulama klasörü&gt;\Scalio.Data`ya gider.
/// Böylece USB'den çalıştırılan Scalio, kurtarılacak diske hiç dokunmaz.
///
/// ⚠️ ORTAM DEĞİŞKENİ İLE AYARLANMAZ ve bu bilinçli. Bu yol, yükseltilmiş
/// sürecin kabul ettiği iş dosyası klasörünü de belirliyor; ortam değişkeniyle
/// yönlendirilebilir olsaydı klasik bir yetki yükseltme kapısı açılırdı. Yol
/// yalnızca ÇALIŞAN İKİLİNİN KONUMUNDAN türetilir — uygulamanın yanına
/// yazabilen biri zaten ikiliyi değiştirebilir, yani yeni bir yetki kazanmaz.
/// </summary>
public static class ScalioPaths
{
    /// <summary>Taşınabilir modu açan işaret dosyası.</summary>
    public const string PortableMarker = "scalio.portable";

    /// <summary>Taşınabilir modda kullanılan klasör adı.</summary>
    public const string PortableFolder = "Scalio.Data";

    private static string? _cached;

    /// <summary>Oturum, abonelik önbelleği, iş dosyaları ve tarayıcı profili buraya yazılır.</summary>
    public static string DataRoot => _cached ??= Resolve();

    /// <summary>Yükseltilmiş sürecin iş dosyaları.</summary>
    public static string JobsRoot => Path.Combine(DataRoot, "jobs");

    /// <summary>Taşınabilir mod etkin mi (arayüzde gösterilir).</summary>
    public static bool IsPortable => PortableRoot() is not null;

    private static string Resolve()
    {
        var portable = PortableRoot();

        if (portable is not null)
        {
            return portable;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Scalio");
    }

    /// <summary>
    /// Taşınabilir kök — yalnızca işaret dosyası varsa VE oraya yazılabiliyorsa.
    /// </summary>
    /// <remarks>
    /// ⚠️ Yazılabilirlik GERÇEKTEN denenir. "Program Files" altına kurulmuş bir
    /// Scalio'nun yanına işaret dosyası konursa yol geçerli görünür ama her
    /// yazma başarısız olur; uygulama açılışta oturum kaydedemez ve sebebi
    /// hiçbir yerde görünmezdi.
    /// </remarks>
    private static string? PortableRoot()
    {
        try
        {
            var executable = Environment.ProcessPath;

            if (executable is null)
            {
                return null;
            }

            var directory = Path.GetDirectoryName(executable);

            if (directory is null || !File.Exists(Path.Combine(directory, PortableMarker)))
            {
                return null;
            }

            var root = Path.Combine(directory, PortableFolder);

            Directory.CreateDirectory(root);

            var probe = Path.Combine(root, ".yazilabilir");
            File.WriteAllText(probe, "1");
            File.Delete(probe);

            return root;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException
                                      or ArgumentException or NotSupportedException)
        {
            // Yazılamıyor: sessizce normal konuma düşülür, uygulama çalışmaya devam eder.
            return null;
        }
    }
}
