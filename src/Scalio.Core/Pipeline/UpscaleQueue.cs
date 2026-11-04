using Scalio.Core.Diagnostics;

namespace Scalio.Core.Pipeline;

/// <summary>Bir kuyruk öğesinin durumu.</summary>
public enum JobStatus
{
    Queued,
    Processing,
    Completed,
    Failed,
    Cancelled,
}

/// <summary>Bir işin durum değişikliği bildirimi.</summary>
public sealed class JobStatusChangedEventArgs : EventArgs
{
    public required Guid JobId { get; init; }

    public required UpscaleJob Job { get; init; }

    public required JobStatus Status { get; init; }

    public string? Error { get; init; }

    public UpscaleResult? Result { get; init; }
}

/// <summary>Kuyruğun toplam ilerlemesi.</summary>
public sealed record QueueProgress(int Total, int Completed);

/// <summary>
/// Birden çok `UpscaleJob`'ı SIRAYLA işler.
///
/// ⚠️ NEDEN SIRALI: GPU tek bir kaynak — aynı anda birden çok motor süreci
/// çalıştırmak, her biri kendi tile'ıyla VRAM'i paylaşmaya çalışır ve OOM
/// olasılığını normalde gerekenden çok artırır. Kuyruk bilerek paralel
/// çalıştırmaz.
/// </summary>
public sealed class UpscaleQueue
{
    private readonly UpscalePipeline _pipeline;
    private readonly List<Entry> _entries = new();

    public UpscaleQueue(UpscalePipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public event EventHandler<JobStatusChangedEventArgs>? StatusChanged;

    public event EventHandler<QueueProgress>? ProgressChanged;

    /// <summary>İşi kuyruğa ekler, kimliğini döner (tekil iptal için kullanılır).</summary>
    public Guid Enqueue(UpscaleJob job)
    {
        var entry = new Entry(Guid.NewGuid(), job);

        _entries.Add(entry);

        Raise(entry, JobStatus.Queued);

        return entry.Id;
    }

    /// <summary>Tek bir işi iptal eder (sırada bekliyorsa hiç başlamaz, işleniyorsa durur).</summary>
    public void CancelJob(Guid jobId)
    {
        var entry = _entries.Find(e => e.Id == jobId);

        entry?.Cts.Cancel();
    }

    /// <summary>Kuyruğun tamamını iptal eder — henüz başlamamış işler de dahil.</summary>
    public void CancelAll()
    {
        foreach (var entry in _entries)
        {
            entry.Cts.Cancel();
        }
    }

    /// <summary>Kuyruktaki işleri sırayla işler.</summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var total = _entries.Count;
        var completed = 0;

        foreach (var entry in _entries)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, entry.Cts.Token);

            if (linked.IsCancellationRequested)
            {
                Raise(entry, JobStatus.Cancelled);
            }
            else
            {
                Raise(entry, JobStatus.Processing);

                try
                {
                    var result = await _pipeline.RunAsync(entry.Job, linked.Token).ConfigureAwait(false);

                    Raise(entry, JobStatus.Completed, result: result);
                }
                catch (OperationCanceledException)
                {
                    Raise(entry, JobStatus.Cancelled);
                }
                catch (Exception e)
                {
                    ScalioLog.Error("UpscaleQueue", "iş başarısız oldu", e);

                    Raise(entry, JobStatus.Failed, error: e.Message);
                }
            }

            completed++;

            ProgressChanged?.Invoke(this, new QueueProgress(total, completed));
        }
    }

    private void Raise(Entry entry, JobStatus status, string? error = null, UpscaleResult? result = null)
    {
        entry.Status = status;

        StatusChanged?.Invoke(this, new JobStatusChangedEventArgs
        {
            JobId = entry.Id,
            Job = entry.Job,
            Status = status,
            Error = error,
            Result = result,
        });
    }

    private sealed class Entry(Guid id, UpscaleJob job)
    {
        public Guid Id { get; } = id;

        public UpscaleJob Job { get; } = job;

        public JobStatus Status { get; set; } = JobStatus.Queued;

        public CancellationTokenSource Cts { get; } = new();
    }
}
