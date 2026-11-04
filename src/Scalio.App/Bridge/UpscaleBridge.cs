using System.Diagnostics;
using System.IO;
using Scalio.Core;
using Scalio.Core.Diagnostics;
using Scalio.Core.Engine;
using Scalio.Core.Pipeline;
using Scalio.Core.Support;

namespace Scalio.App.Bridge;

/// <summary>
/// Arayüz ile büyütme motoru arasındaki tek kapı.
///
/// ⚠️ HER İŞ TEK SIRAYLA İŞLENİR (bkz. `UpscaleQueue` üstündeki not: GPU tek
/// kaynak). Bu sınıf, arayüzden anlık gelen `enqueueJob` isteklerini kendi
/// bekleme kuyruğunda tutar ve HER İŞ İÇİN AYRI, TEK ÖĞELİ bir
/// `UpscaleQueue` çalıştırır — `UpscaleQueue._entries` bir `List&lt;T&gt;`
/// olduğundan, bir `RunAsync` sürerken aynı listeye yeni bir iş eklemek
/// `InvalidOperationException` (koleksiyon değişti) fırlatırdı. Tek öğelik
/// kuyruklar bu riski tamamen ortadan kaldırıyor; motorun kendisi yine de
/// yalnızca bir işi aynı anda çalıştırıyor.
/// </summary>
public sealed class UpscaleBridge
{
    private readonly UpscalePipeline _pipeline;

    private readonly object _lock = new();
    private readonly List<JobRecord> _jobs = new();
    private readonly Queue<(JobRecord Record, UpscaleJob Job)> _pending = new();

    private UpscaleQueue? _currentQueue;
    private Guid _currentInternalId;
    private bool _isProcessing;

    private Action<object>? _pushEvent;

    public UpscaleBridge()
    {
        var enginePath = Path.Combine(ScalioOptions.EnginePath, "realesrgan-ncnn-vulkan.exe");
        var modelsPath = Path.Combine(ScalioOptions.EnginePath, "models");

        _pipeline = new UpscalePipeline(new EsrganUpscaleEngine(enginePath, modelsPath));
    }

    /// <summary>Kuyruk olaylarının WebView2'ye `postMessage` ile iletileceği geri çağrı.</summary>
    public void SetPushHandler(Action<object> push) => _pushEvent = push;

    public async Task<BridgeResponse> HandleAsync(BridgeRequest request)
    {
        // ⚠️ İSTEĞİN İÇERİĞİ YAZILMAZ, yalnız eylem adı: gövdede dosya
        // yolları geçiyor ve bunlar günlüğe düşmemeli (bkz. EsrganRunner).
        ScalioLog.Debug("bridge", $"→ {request.Action}");

        var started = DateTime.Now;

        try
        {
            return request.Action switch
            {
                // ⚠️ Giriş, deneme ve abonelik YOK (2026-11-04): Scalio ücretsiz
                // ve hesapsız. Arayüz yalnız sürüm satırı için sorar.
                "app_info" => Ok(request, new { version = UpdateChecker.CurrentVersion }),
                "open_url" => OpenExternalUrl(request),

                "pick_files" => PickFiles(request),
                "pick_folder" => PickFolder(request),
                "get_thumbnail" => GetThumbnail(request),

                "enqueue_job" => EnqueueJob(request),
                "cancel_job" => CancelJob(request),
                "get_queue_state" => GetQueueState(request),
                "open_in_folder" => OpenInFolder(request),

                "create_support_bundle" => CreateSupportBundle(request),
                "open_log_folder" => OpenLogFolder(request),
                "check_update" => Ok(request, await new UpdateChecker().CheckAsync()),

                _ => Error(request, $"Unknown request: {request.Action}"),
            };
        }
        catch (Exception e)
        {
            ScalioLog.Error("bridge", $"'{request.Action}' beklenmedik hatayla bitti", e);

            return Error(request, e.Message);
        }
        finally
        {
            ScalioLog.Debug("bridge", $"← {request.Action} ({(DateTime.Now - started).TotalMilliseconds:F0} ms)");
        }
    }

    // ── Dosya seçimi ─────────────────────────────────────────────────────────

    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tif", ".tiff"];

    private static BridgeResponse PickFiles(BridgeRequest request)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Görsel seç",
            Multiselect = true,
            Filter = "Görseller|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.tif;*.tiff|Tüm dosyalar|*.*",
        };

        return Ok(request, new { paths = dialog.ShowDialog() == true ? dialog.FileNames : [] });
    }

    /// <summary>
    /// Ayarlanmayı bekleyen bir görsel için küçük bir önizleme üretir.
    ///
    /// ⚠️ ARAYÜZ KAYNAK DOSYAYA DOĞRUDAN ERİŞEMEZ: WebView2'nin CSP'si
    /// `file://` kaynaklarını yasaklıyor (bkz. index.html'deki not) — bu
    /// yüzden önizleme buradan, küçük bir `data:` URI olarak dönüyor.
    /// ⚠️ TAM BOYUT DEĞİL: 480 px'in üstündeki uzun kenar küçültülür ve
    /// JPEG'e (kalite 72) dönüştürülür — bir kuyruk kartı için orijinal
    /// çözünürlük gereksiz, yalnızca aktarımı ve belleği şişirir.
    /// </summary>
    private static BridgeResponse GetThumbnail(BridgeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Path) || !File.Exists(request.Path))
        {
            return Error(request, "Dosya bulunamadı.");
        }

        try
        {
            using var image = new ImageMagick.MagickImage(request.Path);

            const uint maxEdge = 480;

            if (image.Width > maxEdge || image.Height > maxEdge)
            {
                image.FilterType = ImageMagick.FilterType.Lanczos;
                image.Resize(new ImageMagick.MagickGeometry(maxEdge, maxEdge) { IgnoreAspectRatio = false });
            }

            image.Format = ImageMagick.MagickFormat.Jpg;
            image.Quality = 72;

            var bytes = image.ToByteArray();

            return Ok(request, new { dataUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}" });
        }
        catch (Exception e) when (e is ImageMagick.MagickException or IOException)
        {
            // ⚠️ Bozuk/desteklenmeyen dosya önizleme üretemeyebilir — bu bir
            // arıza değil, kuyruğa eklemeyi ENGELLEMEZ. Arayüz genel bir
            // simge gösterir.
            return Error(request, "Önizleme üretilemedi.");
        }
    }

    private static BridgeResponse PickFolder(BridgeRequest request)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Klasör seç" };

        if (dialog.ShowDialog() != true)
        {
            return Ok(request, new { paths = Array.Empty<string>() });
        }

        var paths = Directory.EnumerateFiles(dialog.FolderName)
            .Where(p => ImageExtensions.Contains(Path.GetExtension(p).ToLowerInvariant()))
            .ToArray();

        return Ok(request, new { paths });
    }

    // ── Kuyruk ───────────────────────────────────────────────────────────────

    private sealed class JobRecord
    {
        public required Guid Id { get; init; }

        public required string FileName { get; init; }

        public required string SourcePath { get; init; }

        public string Status { get; set; } = "queued";

        public string? Error { get; set; }

        public string? ResultPath { get; set; }
    }

    private static object Describe(JobRecord record) => new
    {
        id = record.Id.ToString(),
        fileName = record.FileName,
        sourcePath = record.SourcePath,
        status = record.Status,
        error = record.Error,
        resultPath = record.ResultPath,
    };

    private BridgeResponse EnqueueJob(BridgeRequest request)
    {
        if (request.Paths is not { Length: > 0 })
        {
            return Error(request, "Büyütülecek dosya verilmedi.");
        }

        var model = request.Model?.ToLowerInvariant() switch
        {
            "anime" or "drawing" or "logo" => EsrganModel.Anime,
            _ => EsrganModel.Photo,
        };

        var format = request.Format?.ToLowerInvariant() switch
        {
            "jpeg" or "jpg" => OutputFormat.Jpeg,
            "webp" => OutputFormat.WebP,
            _ => OutputFormat.Png,
        };

        var crop = request.CropPreset?.ToLowerInvariant() switch
        {
            "hero" => CropPresets.Hero,
            "card" => CropPresets.Card,
            "instagram" => CropPresets.InstagramPortrait,
            "opengraph" => CropPresets.OpenGraph,
            _ => null,
        };

        var added = new List<object>();

        foreach (var path in request.Paths)
        {
            UpscaleJob job;

            try
            {
                // ⚠️ ÇIKTI KAYNAĞIN YANINA yazılır: bu ürünün sunum ekranında
                // ayrı bir "hedef klasör" adımı yok (brief kapsamı dışı
                // bırakıldı — bkz. rapor). "Klasörde göster" bu yüzden her
                // zaman kaynağın klasörünü açar.
                job = UpscaleJob.Create(
                    path,
                    model,
                    format,
                    Path.GetDirectoryName(Path.GetFullPath(path)) ?? Path.GetTempPath(),
                    scale: request.Scale,
                    targetWidth: request.TargetWidth,
                    quality: request.Quality ?? 90,
                    crop: crop);
            }
            catch (ArgumentException e)
            {
                return Error(request, e.Message);
            }

            var record = new JobRecord { Id = Guid.NewGuid(), FileName = Path.GetFileName(path), SourcePath = path };

            lock (_lock)
            {
                _jobs.Add(record);
                _pending.Enqueue((record, job));
            }

            added.Add(Describe(record));
        }

        StartProcessingIfIdle();

        return Ok(request, new { jobs = added });
    }

    private void StartProcessingIfIdle()
    {
        lock (_lock)
        {
            if (_isProcessing)
            {
                return;
            }

            _isProcessing = true;
        }

        _ = Task.Run(ProcessLoopAsync);
    }

    /// <summary>
    /// Kuyruktaki işleri SIRAYLA işler — her iş kendi tek öğelik
    /// `UpscaleQueue`'sunda (bkz. sınıf başındaki not).
    /// </summary>
    private async Task ProcessLoopAsync()
    {
        while (true)
        {
            JobRecord? record = null;
            UpscaleJob? job = null;

            lock (_lock)
            {
                while (_pending.Count > 0)
                {
                    var candidate = _pending.Dequeue();

                    // Hâlâ sırada beklerken iptal edilmiş olabilir.
                    if (candidate.Record.Status == "cancelled")
                    {
                        continue;
                    }

                    record = candidate.Record;
                    job = candidate.Job;

                    break;
                }

                if (record is null)
                {
                    _isProcessing = false;

                    return;
                }
            }

            var queue = new UpscaleQueue(_pipeline);
            var internalId = queue.Enqueue(job!);

            lock (_lock)
            {
                _currentQueue = queue;
                _currentInternalId = internalId;
            }

            var currentRecord = record;

            queue.StatusChanged += (_, e) =>
            {
                switch (e.Status)
                {
                    case JobStatus.Processing:
                        currentRecord.Status = "processing";
                        break;
                    case JobStatus.Completed:
                        currentRecord.Status = "completed";
                        currentRecord.ResultPath = e.Result?.OutputPath;
                        break;
                    case JobStatus.Failed:
                        currentRecord.Status = "failed";
                        currentRecord.Error = e.Error;
                        break;
                    case JobStatus.Cancelled:
                        currentRecord.Status = "cancelled";
                        break;
                    default:
                        // Queued: Enqueue() sırasında zaten "queued" — tekrar
                        // bildirmeye gerek yok.
                        return;
                }

                PushEvent("jobUpdated", Describe(currentRecord));
            };

            try
            {
                await queue.RunAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // ⚠️ `UpscaleQueue.RunAsync` her işin kendi hatasını zaten
                // yakalayıp `Failed` bildiriyor; buraya düşen şey kuyruğun
                // KENDİSİNDE beklenmedik bir kusur demektir.
                ScalioLog.Error("UpscaleBridge", "iş kuyruğu beklenmedik hatayla bitti", e);

                currentRecord.Status = "failed";
                currentRecord.Error = e.Message;

                PushEvent("jobUpdated", Describe(currentRecord));
            }

            lock (_lock)
            {
                _currentQueue = null;
            }
        }
    }

    private BridgeResponse CancelJob(BridgeRequest request)
    {
        if (!Guid.TryParse(request.JobId, out var id))
        {
            return Error(request, "Geçersiz iş kimliği.");
        }

        lock (_lock)
        {
            var record = _jobs.FirstOrDefault(j => j.Id == id);

            if (record is null)
            {
                return Error(request, "İş bulunamadı.");
            }

            if (record.Status == "queued")
            {
                record.Status = "cancelled";
                PushEvent("jobUpdated", Describe(record));
            }
            else if (record.Status == "processing" && _currentQueue is not null && _currentInternalId != Guid.Empty)
            {
                _currentQueue.CancelJob(_currentInternalId);
            }
        }

        return Ok(request, new { ok = true });
    }

    private BridgeResponse GetQueueState(BridgeRequest request)
    {
        lock (_lock)
        {
            return Ok(request, new { jobs = _jobs.Select(Describe).ToArray() });
        }
    }

    private BridgeResponse OpenInFolder(BridgeRequest request)
    {
        if (!Guid.TryParse(request.JobId, out var id))
        {
            return Error(request, "Geçersiz iş kimliği.");
        }

        JobRecord? record;

        lock (_lock)
        {
            record = _jobs.FirstOrDefault(j => j.Id == id);
        }

        var path = record?.ResultPath ?? record?.SourcePath;

        if (path is null || !File.Exists(path))
        {
            return Error(request, "Dosya bulunamadı.");
        }

        try
        {
            // ⚠️ `/select,` ile Gezgin dosyayı SEÇİLİ olarak açar — yalnızca
            // klasörü açıp kullanıcıyı onlarca dosya arasında bırakmaz.
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{Path.GetFullPath(path)}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception e)
        {
            ScalioLog.Warn("bridge", $"Klasör açılamadı: {e.Message}");
        }

        return Ok(request, new { opened = true });
    }

    // ── Destek / güncelleme ──────────────────────────────────────────────────

    private BridgeResponse CreateSupportBundle(BridgeRequest request)
    {
        var path = SupportBundle.Write(new Dictionary<string, string>
        {
            ["Sürüm"] = UpdateChecker.CurrentVersion,
        });

        ScalioLog.Info("support", $"Destek paketi üretildi: {Path.GetFileName(path)}");

        return Ok(request, new { path });
    }

    private static BridgeResponse OpenLogFolder(BridgeRequest request)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = ScalioLog.Directory, UseShellExecute = true });
        }
        catch (Exception e)
        {
            ScalioLog.Warn("support", $"Günlük klasörü açılamadı: {e.Message}");
        }

        return Ok(request, new { path = ScalioLog.Directory });
    }

    // ── Dış bağlantılar ──────────────────────────────────────────────────────

    /// <summary>
    /// ⚠️ YALNIZ http/https VE YALNIZ `talivio.com`. Arayüz yerel
    /// kaynaklardan geliyor ama bu uç, gelecekte arayüze sızacak herhangi bir
    /// metnin `file://` ya da bir kabuk fiili çalıştırmasına açık bir kapı
    /// olurdu.
    /// </summary>
    private static BridgeResponse OpenExternalUrl(BridgeRequest request) => OpenExternal(request, request.Url);

    private static BridgeResponse OpenExternal(BridgeRequest request, string? url)
    {
        if (string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
            || !parsed.Host.EndsWith("talivio.com", StringComparison.OrdinalIgnoreCase))
        {
            ScalioLog.Warn("bridge", "openUrl reddedildi: alan adı dışı");

            return Error(request, "Bu adres açılamıyor.");
        }

        return Ok(request, new { opened = OpenInBrowser(parsed.ToString()) });
    }

    private static bool OpenInBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

            return true;
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }

    private void PushEvent(string name, object data)
        => _pushEvent?.Invoke(new BridgeEvent { Event = name, Data = data });

    private static BridgeResponse Ok(BridgeRequest request, object data)
        => new() { Id = request.Id, Kind = "result", Data = data };

    private static BridgeResponse Error(BridgeRequest request, string message)
        => new() { Id = request.Id, Kind = "error", Message = message };
}
