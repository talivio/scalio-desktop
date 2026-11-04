using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using Scalio.Core.Diagnostics;
using Scalio.Core.Localization;

namespace Scalio.Core.Support;

/// <summary>İndirme doğrulanamadı ya da kurulum başlatılamadı.</summary>
public sealed class UpdateException(string message) : Exception(message);

/// <summary>İndirilmiş ve doğrulanmış kurulum paketi.</summary>
/// <param name="Path">Diskteki yer.</param>
/// <param name="Size">Bayt.</param>
/// <param name="Sha256">Ölçülen özet — sunucununkiyle EŞİT (aksi hâlde bu nesne hiç üretilmez).</param>
public sealed record DownloadedUpdate(string Path, long Size, string Sha256);

/// <summary>
/// Güncellemeyi uygulama içinden indirir, doğrular ve kurar.
///
/// ⚠️ BU SINIF BİR KARARI TERSİNE ÇEVİRİYOR — bilerek ve sınırlarıyla.
/// <see cref="UpdateChecker"/>'daki not hâlâ doğru: imzasız bir ikilinin
/// internetten .exe indirip çalıştırması, antivirüs sezgiselinin
/// (Trojan-Downloader) ta kendisi ve bu depoda o desen Kaspersky'nin ürünü
/// truva atı saymasına yol açtı. Eren bu riski bilerek "tam otomatik"
/// istedi (2026-09-04). Riski sıfırlayamayız; şu üç dizginle sınırlıyoruz:
///
///  1. DOĞRULAMASIZ ÇALIŞTIRMA YOK. Sunucu SHA-256 vermediyse indirilen
///     dosya kurulMAZ, yalnız klasörde gösterilir. Özet indirme SIRASINDA
///     ölçülür ve kurulumdan hemen önce YENİDEN ölçülür (arada dosya
///     değişmiş olabilir).
///  2. YALNIZ talivio.com'DAN. İndirme adresi de, 302'nin vardığı son adres
///     de bu alan adında olmalı; yönlendirme başka yere giderse durulur.
///  3. SESSİZ AMA GÖRÜNÜR: Inno Setup <c>/SILENT</c> ilerleme penceresini
///     gösterir, UAC istemi yine çıkar (Program Files'a kurulum yönetici
///     ister). Kullanıcı Windows'un izin sorduğunu GÖRÜR — "arkada bir şey
///     kuruldu" hissi, bu üründe kabul edilebilir bir deneyim değil.
///
/// ⚠️ Kod imzası alındığında bu sınıf değişmez; risk profili kendiliğinden
/// düşer. O güne kadar Kaspersky/Defender'ın yeniden işaretleme ihtimali
/// gerçek ve bilinçli olarak kabul edilmiş durumda.
/// </summary>
/// <param name="downloadRoot">Paket klasörü; null = uygulamanın veri kökündeki <c>updates</c> (testler geçici klasör verir).</param>
public sealed class UpdateInstaller(HttpClient? http = null, string? downloadRoot = null)
{
    /// <summary>Inno Setup: ilerleme penceresi var, sorular yok, açık Scalio kapatılır, bitince yeniden başlatılmaz.</summary>
    public const string SilentArguments = "/SILENT /CLOSEAPPLICATIONS /NORESTART";

    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(15);

    private readonly HttpClient _http = http ?? new HttpClient { Timeout = Timeout };

    /// <summary>Paketlerin indirildiği klasör — varsayılan, uygulamanın kendi veri kökünde.</summary>
    public string DownloadRoot { get; } = downloadRoot ?? Path.Combine(ScalioPaths.DataRoot, "updates");

    /// <summary>
    /// Paketi indirir ve özetini doğrular.
    /// </summary>
    /// <param name="progress">(indirilen bayt, toplam bayt ya da -1).</param>
    /// <exception cref="UpdateException">Adres/özet yok, alan adı dışı, özet tutmuyor, ağ kesildi.</exception>
    public async Task<DownloadedUpdate> DownloadAsync(
        UpdateStatus status,
        IProgress<(long Done, long Total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (status.DownloadUrl is null || status.Latest is null)
        {
            throw new UpdateException(Strings.Get("update.no_download"));
        }

        if (status.Sha256 is not { Length: 64 } expected)
        {
            throw new UpdateException(Strings.Get("update.no_checksum"));
        }

        GuardHost(status.DownloadUrl);

        Directory.CreateDirectory(DownloadRoot);

        var final = Path.Combine(DownloadRoot, $"Scalio-{SafeVersion(status.Latest)}-setup.exe");
        var partial = final + ".part";

        // Aynı doğrulanmış paket zaten inmişse yeniden indirilmez.
        if (File.Exists(final))
        {
            var existing = await HashFileAsync(final, cancellationToken);

            if (existing == expected)
            {
                ScalioLog.Info("update", "Paket zaten indirilmiş ve doğrulanmış; yeniden indirilmedi.");

                return new DownloadedUpdate(final, new FileInfo(final).Length, existing);
            }

            TryDelete(final);
        }

        ScalioLog.Info("update", $"Paket indiriliyor · sürüm {status.Latest}");

        using var response = await _http.GetAsync(status.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new UpdateException(Strings.Get("update.download_failed", (int)response.StatusCode));
        }

        // ⚠️ 302'nin VARDIĞI adres de denetlenir: yönlendirme alan adı
        // dışına çıktıysa sunucu ya da yol ele geçirilmiş demektir.
        if (response.RequestMessage?.RequestUri is { } landed)
        {
            GuardHost(landed.ToString());
        }

        var total = response.Content.Headers.ContentLength ?? -1;
        var done = 0L;

        using var sha = SHA256.Create();

        try
        {
            await using (var network = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var file = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, useAsync: true))
            {
                var buffer = new byte[1 << 16];
                int read;

                while ((read = await network.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    sha.TransformBlock(buffer, 0, read, null, 0);
                    done += read;
                    progress?.Report((done, total));
                }
            }

            sha.TransformFinalBlock([], 0, 0);
            var measured = Convert.ToHexString(sha.Hash!).ToLowerInvariant();

            /*
             * ⚠️ ÖZET TUTMUYORSA DOSYA SİLİNİR, "yine de kur" seçeneği YOK.
             * Yarım inmiş ya da değiştirilmiş bir paketi kullanıcının önüne
             * "istersen kur" diye koymak, doğrulamanın anlamını yok ederdi.
             */
            if (measured != expected)
            {
                TryDelete(partial);

                ScalioLog.Error("update", $"Özet tutmuyor · beklenen {expected[..12]}… · ölçülen {measured[..12]}…");

                throw new UpdateException(Strings.Get("update.hash_mismatch"));
            }

            File.Move(partial, final, overwrite: true);

            ScalioLog.Info("update", $"Paket indirildi ve doğrulandı · {done} bayt");

            return new DownloadedUpdate(final, done, measured);
        }
        catch (Exception e) when (e is HttpRequestException or IOException or TaskCanceledException)
        {
            TryDelete(partial);

            throw new UpdateException(Strings.Get("update.download_failed", 0) + " " + e.Message);
        }
    }

    /// <summary>
    /// Kurulum dosyasını YENİDEN doğrulayıp başlatır. Dönüşten sonra çağıran
    /// (uygulama) tek-kopya kilidini bırakıp KAPANMALIDIR: kurulum programı
    /// aynı kilide bakar ve dosyaların üzerine yazabilmek için sürecin
    /// gitmesini bekler.
    /// </summary>
    public static async Task StartInstallerAsync(string path, string expectedSha256, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            throw new UpdateException(Strings.Get("update.no_download"));
        }

        var measured = await HashFileAsync(path, cancellationToken);

        if (!string.Equals(measured, expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            TryDelete(path);

            throw new UpdateException(Strings.Get("update.hash_mismatch"));
        }

        try
        {
            /*
             * `UseShellExecute = true`: kurulum programı kendi bildirimiyle
             * (PrivilegesRequired=admin) UAC ister; kabuk üzerinden açmak bu
             * istemin çıkmasını sağlar. Doğrudan CreateProcess "erişim
             * reddedildi" ile düşerdi.
             */
            Process.Start(new ProcessStartInfo(path)
            {
                Arguments = SilentArguments,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(path),
            });

            ScalioLog.Info("update", "Kurulum programı başlatıldı; uygulama kapanıyor.");
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            // UAC reddi de buraya düşer (1223): kurulum olmadı, uygulama açık kalır.
            throw new UpdateException(Strings.Get("update.install_failed", e.Message));
        }
    }

    /// <summary>
    /// ⚠️ YALNIZ talivio.com. Sunucunun verdiği adres bile olsa başka bir alan
    /// adına gitmek, ele geçirilmiş bir yanıtın uygulamayı istediği yerden
    /// .exe indirtmesi demek.
    /// </summary>
    internal static void GuardHost(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            || parsed.Scheme != Uri.UriSchemeHttps
            || !(parsed.Host.Equals("talivio.com", StringComparison.OrdinalIgnoreCase)
                 || parsed.Host.EndsWith(".talivio.com", StringComparison.OrdinalIgnoreCase)))
        {
            throw new UpdateException(Strings.Get("update.untrusted_host"));
        }
    }

    internal static async Task<string> HashFileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16, useAsync: true);

        return Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();
    }

    /// <summary>Sürüm dizgisi dosya adına giriyor; yalnız rakam ve nokta kalır.</summary>
    private static string SafeVersion(string version)
    {
        var safe = new string(version.Where(c => char.IsAsciiDigit(c) || c == '.').ToArray());

        return safe.Length > 0 ? safe : "update";
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Silinemeyen yarım dosya bir sonraki indirmede üzerine yazılır.
        }
    }
}
