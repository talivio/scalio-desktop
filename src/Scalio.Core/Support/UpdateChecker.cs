using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Scalio.Core.Diagnostics;

namespace Scalio.Core.Support;

/// <summary>Sürüm denetiminin sonucu.</summary>
/// <param name="Current">Çalışan sürüm.</param>
/// <param name="Latest">Yayındaki sürüm (bilinemiyorsa null).</param>
/// <param name="UpdateAvailable">Yenisi var mı.</param>
/// <param name="Required">Bu sürüm artık desteklenmiyor mu.</param>
/// <param name="DownloadUrl">İndirme adresi.</param>
/// <param name="Sha256">Kurulum paketinin özeti (hex). YOKSA uygulama içi kurulum KAPALIDIR.</param>
public sealed record UpdateStatus(
    string Current,
    string? Latest,
    bool UpdateAvailable,
    bool Required,
    string? DownloadUrl,
    string? Sha256 = null)
{
    /// <summary>
    /// Uygulama içinden indirip kurabilir miyiz: adres VE özet olmalı.
    /// Özet yoksa indirilen dosya doğrulanamaz ve doğrulanmamış bir .exe'yi
    /// çalıştırmak, bu üründe bedeli ölçülmüş bir hata (bkz. UpdateInstaller).
    /// </summary>
    public bool CanInstall => UpdateAvailable && DownloadUrl is not null && Sha256 is { Length: 64 };
}

/// <summary>
/// Yayındaki sürümü sorar.
///
/// ⚠️ NEDEN OTOMATİK GÜNCELLEME YOK — KARAR. Kendini indirip kuran bir
/// güncelleyici, bu üründe en pahalı özellik olurdu: imzasız bir ikilinin
/// internetten .exe indirip çalıştırması, antivirüslerin Trojan-Downloader
/// imzasının ta kendisi. Bu depoda tam olarak o desen (bir `.bat` köprüsü)
/// Kaspersky'nin Scalio'yu truva atı saymasına yol açtı ve kaldırıldı.
/// Burada yapılan şey yalnız HABER VERMEK; indirmeyi kullanıcı tarayıcıda
/// yapar.
///
/// ⚠️ HATA HİÇBİR ZAMAN KULLANICIYA GÖSTERİLMEZ. Denetim ikincil bir
/// yetenek; ağı olmayan makinede (bu araç sıklıkla öyle bir makinede
/// açılıyor) "güncelleme denetlenemedi" demek, çözemeyeceği bir sorunu
/// kullanıcının önüne koymak olurdu. Başarısızlık sessizce günlüğe düşer.
/// </summary>
public sealed class UpdateChecker(HttpClient? http = null)
{
    /// <summary>
    /// ⚠️ Zaman aşımı KISA. Denetim açılış akışının içinde; yavaş ya da
    /// yarı düşmüş bir ağda uzun bir bekleme, uygulamanın açılışını
    /// geciktirirdi — ikincil bir yetenek için asıl işi durdurmak olurdu.
    /// </summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(6);

    private readonly HttpClient _http = http ?? new HttpClient { Timeout = Timeout };

    /// <summary>Çalışan ikilinin sürümü.</summary>
    public static string CurrentVersion
    {
        get
        {
            try
            {
                return FileVersionInfo
                    .GetVersionInfo(Environment.ProcessPath ?? typeof(UpdateChecker).Assembly.Location)
                    .ProductVersion?.Split('+')[0] ?? "0.0.0";
            }
            catch
            {
                return "0.0.0";
            }
        }
    }

    public async Task<UpdateStatus> CheckAsync(CancellationToken cancellationToken = default)
    {
        var current = CurrentVersion;

        try
        {
            var url = ScalioOptions.RelayUrl.TrimEnd('/') + "/api/version";
            var payload = await _http.GetFromJsonAsync<VersionPayload>(url, cancellationToken);

            if (payload?.Latest is null)
            {
                ScalioLog.Debug("update", "Sunucu sürüm bildirmedi; denetim atlandı.");

                return new UpdateStatus(current, null, false, false, null);
            }

            var newer = Compare(payload.Latest, current) > 0;
            var required = payload.Minimum is not null && Compare(payload.Minimum, current) > 0;

            ScalioLog.Info(
                "update",
                $"Sürüm denetimi · çalışan {current} · yayındaki {payload.Latest}"
                + $"{(newer ? " · YENİSİ VAR" : "")}{(required ? " · ZORUNLU" : "")}");

            return new UpdateStatus(current, payload.Latest, newer, required, payload.Url, Normalise(payload.Sha256));
        }
        catch (Exception e)
        {
            // Ağ yok, sunucu düştü, JSON bozuk: hiçbiri kullanıcının sorunu değil.
            ScalioLog.Debug("update", $"Sürüm denetimi yapılamadı: {e.Message}");

            return new UpdateStatus(current, null, false, false, null);
        }
    }

    /// <summary>
    /// İki sürümü sayısal olarak karşılaştırır (a &gt; b ise pozitif).
    /// </summary>
    /// <remarks>
    /// ⚠️ METİN KARŞILAŞTIRMASI KULLANILMAZ ve sebebi somut: "2.10.0" metin
    /// olarak "2.9.0"dan KÜÇÜKTÜR. Onuncu yamada tüm kullanıcılar sessizce
    /// "güncelsiniz" görürdü — hem de kusurun düzeltildiği sürümde.
    ///
    /// ⚠️ Ayrıştırılamayan parça 0 sayılır ve karşılaştırma yine çalışır:
    /// sunucudan beklenmedik bir metin gelmesi, güncellemeyi kaçırmaktan
    /// daha iyi bir sonuçla ("yenisi yok") biter.
    /// </remarks>
    public static int Compare(string a, string b)
    {
        var left = Parts(a);
        var right = Parts(b);

        for (var index = 0; index < Math.Max(left.Length, right.Length); index++)
        {
            var l = index < left.Length ? left[index] : 0;
            var r = index < right.Length ? right[index] : 0;

            if (l != r)
            {
                return l.CompareTo(r);
            }
        }

        return 0;
    }

    /// <summary>Sunucunun özeti: küçük harf hex, 64 karakter; başka her şey "yok" sayılır.</summary>
    private static string? Normalise(string? sha256)
    {
        var trimmed = sha256?.Trim().ToLowerInvariant();

        return trimmed is { Length: 64 } && trimmed.All(c => char.IsAsciiHexDigitLower(c)) ? trimmed : null;
    }

    private static int[] Parts(string version) =>
        version.Trim().Split('.', '-', '+')
            .Select(part => int.TryParse(part, out var value) ? value : 0)
            .ToArray();

    private sealed record VersionPayload
    {
        [JsonPropertyName("latest")]
        public string? Latest { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }

        [JsonPropertyName("minimum")]
        public string? Minimum { get; init; }

        [JsonPropertyName("sha256")]
        public string? Sha256 { get; init; }
    }
}
