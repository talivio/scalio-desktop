using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Scalio.Core.Diagnostics;

/// <summary>Günlük satırının ağırlığı.</summary>
public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error,
}

/// <summary>
/// Dosyaya yazan basit günlük.
///
/// ⚠️ NEDEN VAR — GERÇEK BİR OLAY. Bir kullanıcının makinesinde derin tarama
/// çalışmadı; ekranda yalnızca "yardımcı süreç sonuç yazmadan kapandı" yazdı
/// ve elimizde başka HİÇBİR ŞEY yoktu. Ne hangi adıma kadar gelindiği, ne
/// hangi birimin açılamadığı, ne de sürecin gerçekten başlayıp başlamadığı
/// bilinebiliyordu. Uzaktan teşhis edilemeyen bir kurtarma aracı, ilk gerçek
/// kusurda desteklenemez hâle geliyor.
///
/// ⚠️ TASARIM KARARLARI (hepsinin bir bedeli ödendi):
///
/// • **Üçüncü taraf günlük paketi YOK.** Serilog/NLog eklemek, imzasız ve
///   zaten antivirüs sürtünmesi yaşayan bir pakete birkaç MB daha DLL koymak
///   demekti; bu üründe her yeni ikili sezgisel taramada yeni bir risk.
///
/// • **Yazma HİÇBİR KOŞULDA fırlatmaz.** Günlük ikincil bir yetenektir;
///   diskin dolu olması ya da klasörün kilitli olması kurtarma işini
///   durdurmamalı. Kurtarma yazılımında "günlük yazılamadı" diye işlemi
///   iptal etmek, asıl işi ikincil bir şey için feda etmek olurdu.
///
/// • **Süreçler arası paylaşımlı.** Yükseltilmiş yardımcı süreç (`--elevated-job`)
///   AYRI bir süreçtir ve asıl ilgilendiğimiz satırlar oradan geliyor. Dosya
///   `FileShare.ReadWrite` ile açılır ve her satır ayrı açılış/kapanışla
///   yazılır — iki süreç aynı dosyaya güvenle yazabilsin diye.
/// </summary>
public static class ScalioLog
{
    /// <summary>Tek bir günlük dosyasının üst sınırı (bayt).</summary>
    /// <remarks>
    /// ⚠️ Sınır ŞART: derin tarama bir terabaytlık diskte on binlerce satır
    /// üretebilir. Sınırsız bir günlük, kurtarma aracının kullanıcının diskini
    /// doldurması demekti — hem de tam yer açması gereken anda.
    /// </remarks>
    public const long MaxBytes = 2 * 1024 * 1024;

    /// <summary>Saklanan devredilmiş dosya sayısı.</summary>
    private const int KeptArchives = 3;

    private static readonly object Gate = new();

    private static string? _cachedDirectory;

    /// <summary>Günlük klasörü (taşınabilir modda uygulamanın yanında).</summary>
    public static string Directory => _cachedDirectory ??= EnsureDirectory();

    /// <summary>Etkin günlük dosyası.</summary>
    public static string CurrentFile => Path.Combine(Directory, "scalio.log");

    /// <summary>
    /// Ayrıntı satırları yazılsın mı.
    /// </summary>
    /// <remarks>
    /// ⚠️ Varsayılan KAPALI. Ayrıntı seviyesi her MFT kaydını yazacak kadar
    /// gürültülü; her kullanıcıda açık olması hem diski hem de gerçekten
    /// önemli satırların görünürlüğünü tüketirdi. Destek gerektiğinde
    /// `scalio.debug` işaret dosyasıyla açılır.
    /// </remarks>
    public static bool VerboseEnabled { get; private set; }

    /// <summary>
    /// Süreç açılışında bir kez: klasörü hazırlar, devri yapar ve künyeyi yazar.
    /// </summary>
    /// <param name="role">"app" ya da "elevated" — satırlar hangi süreçten.</param>
    public static void Start(string role)
    {
        try
        {
            VerboseEnabled = File.Exists(Path.Combine(Directory, "scalio.debug"));

            Roll();

            var version = FileVersionInfo
                .GetVersionInfo(Environment.ProcessPath ?? typeof(ScalioLog).Assembly.Location)
                .ProductVersion?.Split('+')[0] ?? "?";

            Write(LogLevel.Info, role, "─────────────────────────────────────────────");
            Write(LogLevel.Info, role, $"Scalio {version} · {Environment.OSVersion.VersionString} · "
                                       + $"{RuntimeInformation.ProcessArchitecture} · pid {Environment.ProcessId}");
        }
        catch
        {
            // Günlük kurulamadıysa uygulama yine de çalışır.
        }
    }

    public static void Debug(string source, string message)
    {
        if (VerboseEnabled)
        {
            Write(LogLevel.Debug, source, message);
        }
    }

    public static void Info(string source, string message) => Write(LogLevel.Info, source, message);

    public static void Warn(string source, string message) => Write(LogLevel.Warn, source, message);

    /// <summary>
    /// Hata satırı — istisna verilirse türü, mesajı ve yığını da yazılır.
    /// </summary>
    /// <remarks>
    /// ⚠️ Yığın izi YAZILIR ve bu bilinçli: kullanıcıya gösterilen cümle
    /// sadeleştirilmiş oluyor ("dosya okunamadı"), oysa teşhis için gereken
    /// şey hangi katmanda hangi Win32 hatasının döndüğü.
    /// </remarks>
    public static void Error(string source, string message, Exception? exception = null)
    {
        if (exception is null)
        {
            Write(LogLevel.Error, source, message);

            return;
        }

        var builder = new StringBuilder(message)
            .Append(" · ")
            .Append(exception.GetType().Name)
            .Append(": ")
            .Append(exception.Message);

        if (exception is System.ComponentModel.Win32Exception win32)
        {
            builder.Append(" (Win32 ").Append(win32.NativeErrorCode).Append(')');
        }

        if (exception.StackTrace is { Length: > 0 } stack)
        {
            builder.Append('\n').Append(stack);
        }

        Write(LogLevel.Error, source, builder.ToString());
    }

    /// <summary>Günlük dosyalarını yeniden okunabilir tek metin olarak verir.</summary>
    /// <remarks>
    /// ⚠️ ESKİDEN YENİYE sıralanır: destek okuyan kişi olayı akış hâlinde
    /// görmeli, tersten değil.
    /// </remarks>
    public static string ReadAll()
    {
        try
        {
            var builder = new StringBuilder();

            for (var index = KeptArchives; index >= 1; index--)
            {
                var archive = Path.Combine(Directory, $"scalio.{index}.log");

                if (File.Exists(archive))
                {
                    builder.Append(ReadShared(archive));
                }
            }

            builder.Append(ReadShared(CurrentFile));

            return builder.ToString();
        }
        catch (Exception e)
        {
            return $"(günlük okunamadı: {e.Message})";
        }
    }

    /// <summary>Tüm günlükleri siler (kullanıcının isteğiyle).</summary>
    public static void Clear()
    {
        lock (Gate)
        {
            try
            {
                foreach (var file in System.IO.Directory.GetFiles(Directory, "scalio*.log"))
                {
                    File.Delete(file);
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Kilitli dosya silinemez; kalanı silinmiş olur.
            }
        }
    }

    private static void Write(LogLevel level, string source, string message)
    {
        var line = string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} {Tag(level)} [{source}] {message}\n");

        lock (Gate)
        {
            try
            {
                /*
                 * ⚠️ Her satırda AÇ-YAZ-KAPAT ve `FileShare.ReadWrite`.
                 * Yükseltilmiş yardımcı süreç ayrı bir süreçtir ve aynı dosyaya
                 * yazıyor; açık tutulan bir akış, iki süreçten birinin dosyayı
                 * hiç açamaması demekti. Asıl teşhis satırları o süreçten
                 * geldiği için bu takas bilinçli.
                 */
                using var stream = new FileStream(
                    CurrentFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

                var bytes = Encoding.UTF8.GetBytes(line);

                stream.Write(bytes, 0, bytes.Length);
            }
            catch
            {
                // Günlük yazılamıyorsa sessiz kalınır — asıl iş durmamalı.
            }
        }
    }

    private static string Tag(LogLevel level) => level switch
    {
        LogLevel.Debug => "DBG",
        LogLevel.Info => "INF",
        LogLevel.Warn => "WRN",
        LogLevel.Error => "ERR",
        _ => "???",
    };

    /// <summary>Dosya büyüdüyse devreder: scalio.log → scalio.1.log → …</summary>
    private static void Roll()
    {
        lock (Gate)
        {
            try
            {
                if (!File.Exists(CurrentFile) || new FileInfo(CurrentFile).Length < MaxBytes)
                {
                    return;
                }

                var oldest = Path.Combine(Directory, $"scalio.{KeptArchives}.log");

                if (File.Exists(oldest))
                {
                    File.Delete(oldest);
                }

                for (var index = KeptArchives - 1; index >= 1; index--)
                {
                    var from = Path.Combine(Directory, $"scalio.{index}.log");

                    if (File.Exists(from))
                    {
                        File.Move(from, Path.Combine(Directory, $"scalio.{index + 1}.log"), overwrite: true);
                    }
                }

                File.Move(CurrentFile, Path.Combine(Directory, "scalio.1.log"), overwrite: true);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Devir yapılamadıysa aynı dosyaya yazmaya devam edilir.
            }
        }
    }

    /// <summary>
    /// ⚠️ Okuma PAYLAŞIMLI açılır: dosyaya o anda yazan (yükseltilmiş) bir
    /// süreç varken "başka bir işlem tarafından kullanılıyor" hatası almak,
    /// destek paketini tam ihtiyaç anında üretilemez kılardı.
    /// </summary>
    private static string ReadShared(string path)
    {
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        return reader.ReadToEnd();
    }

    private static string EnsureDirectory()
    {
        var directory = Path.Combine(ScalioPaths.DataRoot, "logs");

        System.IO.Directory.CreateDirectory(directory);

        return directory;
    }
}
