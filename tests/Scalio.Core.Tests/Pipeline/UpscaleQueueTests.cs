using ImageMagick;
using Scalio.Core.Engine;
using Scalio.Core.Pipeline;

namespace Scalio.Core.Tests.Pipeline;

public sealed class UpscaleQueueTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "ScalioTests", Guid.NewGuid().ToString("N"));
    private readonly string _outputFolder;

    public UpscaleQueueTests()
    {
        Directory.CreateDirectory(_tempDirectory);
        _outputFolder = Path.Combine(_tempDirectory, "out");
        Directory.CreateDirectory(_outputFolder);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private string MakeSource(string name)
    {
        var path = Path.Combine(_tempDirectory, name);
        using var image = new MagickImage(MagickColors.SkyBlue, 20, 20);
        image.Write(path);

        return path;
    }

    [Fact]
    public async Task Processes_jobs_sequentially_in_order()
    {
        var order = new List<string>();
        var engine = new FakeUpscaleEngine
        {
            Behavior = async (input, output, scale, ct) =>
            {
                order.Add(Path.GetFileName(input));
                await Task.Delay(10, ct);
                File.Copy(input, output, overwrite: true);
            },
        };

        var pipeline = new UpscalePipeline(engine);
        var queue = new UpscaleQueue(pipeline);

        queue.Enqueue(UpscaleJob.Create(MakeSource("a.png"), EsrganModel.Photo, OutputFormat.Png, _outputFolder, scale: 2));
        queue.Enqueue(UpscaleJob.Create(MakeSource("b.png"), EsrganModel.Photo, OutputFormat.Png, _outputFolder, scale: 2));
        queue.Enqueue(UpscaleJob.Create(MakeSource("c.png"), EsrganModel.Photo, OutputFormat.Png, _outputFolder, scale: 2));

        await queue.RunAsync();

        Assert.Equal(new[] { "a.png", "b.png", "c.png" }, order);
    }

    [Fact]
    public async Task Reports_status_and_progress_for_each_job()
    {
        var engine = new FakeUpscaleEngine();
        var pipeline = new UpscalePipeline(engine);
        var queue = new UpscaleQueue(pipeline);

        var statuses = new List<JobStatus>();
        var progresses = new List<QueueProgress>();

        queue.StatusChanged += (_, e) => statuses.Add(e.Status);
        queue.ProgressChanged += (_, p) => progresses.Add(p);

        queue.Enqueue(UpscaleJob.Create(MakeSource("a.png"), EsrganModel.Photo, OutputFormat.Png, _outputFolder, scale: 2));
        queue.Enqueue(UpscaleJob.Create(MakeSource("b.png"), EsrganModel.Photo, OutputFormat.Png, _outputFolder, scale: 2));

        await queue.RunAsync();

        Assert.Equal(
            new[] { JobStatus.Queued, JobStatus.Queued, JobStatus.Processing, JobStatus.Completed, JobStatus.Processing, JobStatus.Completed },
            statuses);

        Assert.Equal(new QueueProgress(2, 1), progresses[0]);
        Assert.Equal(new QueueProgress(2, 2), progresses[1]);
    }

    [Fact]
    public async Task Failed_job_is_reported_but_does_not_stop_the_queue()
    {
        var callIndex = 0;
        var engine = new FakeUpscaleEngine
        {
            Behavior = (input, output, scale, ct) =>
            {
                callIndex++;

                if (callIndex == 1)
                {
                    throw new EngineFailedException("motor çöktü");
                }

                File.Copy(input, output, overwrite: true);

                return Task.CompletedTask;
            },
        };

        var pipeline = new UpscalePipeline(engine);
        var queue = new UpscaleQueue(pipeline);

        var statuses = new List<JobStatus>();
        queue.StatusChanged += (_, e) => statuses.Add(e.Status);

        queue.Enqueue(UpscaleJob.Create(MakeSource("a.png"), EsrganModel.Photo, OutputFormat.Png, _outputFolder, scale: 2));
        queue.Enqueue(UpscaleJob.Create(MakeSource("b.png"), EsrganModel.Photo, OutputFormat.Png, _outputFolder, scale: 2));

        await queue.RunAsync();

        Assert.Contains(JobStatus.Failed, statuses);
        Assert.Contains(JobStatus.Completed, statuses);
    }

    [Fact]
    public async Task Cancelling_a_single_job_skips_only_that_job()
    {
        var engine = new FakeUpscaleEngine
        {
            Behavior = (input, output, scale, ct) =>
            {
                File.Copy(input, output, overwrite: true);

                return Task.CompletedTask;
            },
        };

        var pipeline = new UpscalePipeline(engine);
        var queue = new UpscaleQueue(pipeline);

        var jobAId = queue.Enqueue(UpscaleJob.Create(MakeSource("a.png"), EsrganModel.Photo, OutputFormat.Png, _outputFolder, scale: 2));
        queue.Enqueue(UpscaleJob.Create(MakeSource("b.png"), EsrganModel.Photo, OutputFormat.Png, _outputFolder, scale: 2));

        queue.CancelJob(jobAId);

        var results = new List<(Guid Id, JobStatus Status)>();
        queue.StatusChanged += (_, e) => results.Add((e.JobId, e.Status));

        await queue.RunAsync();

        Assert.Contains(results, r => r.Id == jobAId && r.Status == JobStatus.Cancelled);
        Assert.Contains(results, r => r.Status == JobStatus.Completed);
    }

    [Fact]
    public async Task Cancelling_the_whole_queue_cancels_all_pending_jobs()
    {
        var engine = new FakeUpscaleEngine
        {
            Behavior = (input, output, scale, ct) =>
            {
                File.Copy(input, output, overwrite: true);

                return Task.CompletedTask;
            },
        };

        var pipeline = new UpscalePipeline(engine);
        var queue = new UpscaleQueue(pipeline);

        queue.Enqueue(UpscaleJob.Create(MakeSource("a.png"), EsrganModel.Photo, OutputFormat.Png, _outputFolder, scale: 2));
        queue.Enqueue(UpscaleJob.Create(MakeSource("b.png"), EsrganModel.Photo, OutputFormat.Png, _outputFolder, scale: 2));

        queue.CancelAll();

        var statuses = new List<JobStatus>();
        queue.StatusChanged += (_, e) => statuses.Add(e.Status);

        await queue.RunAsync();

        Assert.All(statuses, status => Assert.Equal(JobStatus.Cancelled, status));
    }
}
