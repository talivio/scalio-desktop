using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scalio.Shell;

public static class BridgeJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

/// <summary>
/// Arayüzden gelen istek.
///
/// ⚠️ `Id` ZORUNLU: arayüz aynı anda birden çok iş başlatabilir (tarama
/// sürerken hedef klasör seçmek gibi) ve yanıtların hangi isteğe ait olduğu
/// yalnızca buradan bilinir. Kimliksiz bir protokol, geç dönen bir yanıtın
/// yanlış ekranı güncellemesi demekti.
/// </summary>
public sealed class BridgeRequest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    [JsonPropertyName("drive")]
    public string? Drive { get; set; }

    [JsonPropertyName("destination")]
    public string? Destination { get; set; }

    [JsonPropertyName("extensions")]
    public string[]? Extensions { get; set; }

    [JsonPropertyName("ids")]
    public string[]? Ids { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    /// <summary>
    /// Tarayıcıda açılacak adres (`openUrl`).
    /// </summary>
    /// <remarks>
    /// ⚠️ DOĞRULANMADAN KULLANILMAZ — şema ve alan adı köprüde denetleniyor
    /// (bkz. <c>OpenExternalUrl</c>). Buradan gelen ham metni doğrudan kabuğa
    /// vermek, keyfi program çalıştırmanın yolu olurdu.
    /// </remarks>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>updateInstall: indirilmiş paketin yolu (updateDownload'ın döndürdüğü).</summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>updateInstall: paketin beklenen SHA-256'sı — kurulumdan önce YENİDEN ölçülür.</summary>
    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }

    // ── Büyütme köprüsü (bkz. UpscaleBridge) ────────────────────────────────

    /// <summary>enqueueJob: büyütülecek kaynak dosyaların tam yolları.</summary>
    [JsonPropertyName("paths")]
    public string[]? Paths { get; set; }

    /// <summary>enqueueJob: sabit motor ölçeği (2/3/4). `targetWidth` ile birlikte olamaz.</summary>
    [JsonPropertyName("scale")]
    public int? Scale { get; set; }

    /// <summary>enqueueJob: hedef genişlik (px). `scale` ile birlikte olamaz.</summary>
    [JsonPropertyName("targetWidth")]
    public int? TargetWidth { get; set; }

    /// <summary>enqueueJob: "photo" | "anime".</summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>enqueueJob: "png" | "jpeg" | "webp".</summary>
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    /// <summary>enqueueJob: JPEG/WebP kalitesi (1-100).</summary>
    [JsonPropertyName("quality")]
    public int? Quality { get; set; }

    /// <summary>enqueueJob: "hero" | "card" | "instagram" | "opengraph" | null (yok).</summary>
    [JsonPropertyName("cropPreset")]
    public string? CropPreset { get; set; }

    /// <summary>stageDroppedFile: bırakılan dosyanın adı (yol değil, yalnız ad).</summary>
    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    /// <summary>stageDroppedFile: bırakılan dosyanın baytları, base64.</summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }

    /// <summary>cancelJob / openInFolder: hedef işin köprü kimliği.</summary>
    [JsonPropertyName("jobId")]
    public string? JobId { get; set; }
}

// NOT: `DeepCandidate` (Salvio'daki T1/T2 aday sınıfı) buraya taşınmadı —
// `Scalio.Core.Model`/`Recovery` kopyalanmadı, motor entegrasyonu sonraki
// adımda ayrı bir brief ile gelecek.

/// <summary>Bir işin sonucu ya da ilerlemesi.</summary>
public sealed class BridgeResponse
{
    public string Id { get; init; } = "";

    /// "result" | "progress" | "error"
    public string Kind { get; init; } = "result";

    public string? Message { get; init; }

    /// <summary>
    /// ⚠️ Hata "yükseltme gerekiyor" ise arayüz bunu KIRMIZI HATA olarak
    /// göstermez, UAC isteği olarak gösterir. İkisi kullanıcı için tamamen
    /// farklı şeyler: biri arıza, diğeri yapılacak bir iş.
    /// </summary>
    public bool NeedsElevation { get; init; }

    public object? Data { get; init; }
}

/// <summary>
/// İstek-cevap eşleşmesi OLMAYAN tek yönlü bildirim (ör. kuyruk durumu).
///
/// ⚠️ `Id` YOK, BİLEREK: bu mesaj hiçbir isteğin cevabı değil — arayüz onu
/// bekleyen bir istek eşlemesinde değil, ayrı bir olay dinleyicisinde
/// karşılar (bkz. app.js `onEvent`). Kuyruk durumunu yoklama (polling) ile
/// değil, iş durumu değiştikçe anında göndermek için var.
/// </summary>
public sealed class BridgeEvent
{
    public string Kind { get; } = "event";

    public required string Event { get; init; }

    public object? Data { get; init; }
}
