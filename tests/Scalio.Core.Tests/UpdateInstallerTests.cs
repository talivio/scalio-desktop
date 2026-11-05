using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Scalio.Core;
using Scalio.Core.Support;

namespace Scalio.Core.Tests;

/// <summary>
/// Uygulama içi güncelleme — indirme ve doğrulama.
///
/// ⚠️ Bu testler GÜVENLİK SINIRLARINI kilitliyor: özetsiz paket kurulmaz,
/// özeti tutmayan paket silinir, talivio.com dışına gidilmez. Kurulum
/// programının kendisi (Process.Start) burada çalıştırılmaz.
/// </summary>
public sealed class UpdateInstallerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"scalio-upd-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static readonly byte[] Package = Encoding.ASCII.GetBytes("MZ fake installer bytes ".PadRight(5000, 'x'));

    private static string Sha(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    private sealed class FakeDownload(byte[] body, string? landOn = null, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;

            var response = new HttpResponseMessage(status)
            {
                Content = new ByteArrayContent(body),
                RequestMessage = landOn is null ? request : new HttpRequestMessage(HttpMethod.Get, landOn),
            };

            response.Content.Headers.ContentLength = body.Length;

            return Task.FromResult(response);
        }
    }

    private UpdateInstaller Installer(HttpMessageHandler handler) => new(new HttpClient(handler), _root);

    private static UpdateStatus Status(string? sha, string url = "https://scalio.talivio.com/download")
        => new("2.3.0", "2.4.0", UpdateAvailable: true, Required: false, url, sha);

    [Fact]
    public async Task A_package_whose_checksum_matches_is_saved_and_reported()
    {
        var handler = new FakeDownload(Package);
        var progress = new List<(long, long)>();

        var result = await Installer(handler).DownloadAsync(Status(Sha(Package)), new InlineProgressList(progress));

        Assert.True(File.Exists(result.Path));
        Assert.Equal(Package.Length, result.Size);
        Assert.Equal(Sha(Package), result.Sha256);
        Assert.EndsWith("Scalio-2.4.0-setup.exe", result.Path);
        Assert.Equal(Package, await File.ReadAllBytesAsync(result.Path));
        Assert.NotEmpty(progress);
        Assert.Equal(Package.Length, progress[^1].Item1);
    }

    /// <summary>
    /// ⚠️ EN KRİTİK TEST. Özet tutmuyorsa dosya SİLİNİR ve "yine de kur" yok.
    /// </summary>
    [Fact]
    public async Task A_package_whose_checksum_differs_is_deleted_and_refused()
    {
        var handler = new FakeDownload(Package);

        var e = await Assert.ThrowsAsync<UpdateException>(() =>
            Installer(handler).DownloadAsync(Status(Sha(Encoding.ASCII.GetBytes("başka içerik")))));

        Assert.Contains("checksum", e.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(_root) && Directory.EnumerateFiles(_root).Any(), "indirme klasöründe dosya kalmamalı");
    }

    /// <summary>Sunucu özet vermediyse indirme bile başlamaz.</summary>
    [Fact]
    public async Task Without_a_published_checksum_nothing_is_downloaded()
    {
        var handler = new FakeDownload(Package);

        await Assert.ThrowsAsync<UpdateException>(() => Installer(handler).DownloadAsync(Status(sha: null)));

        Assert.Equal(0, handler.Requests);
    }

    [Theory]
    [InlineData("https://evil.example/Scalio-setup.exe")]
    [InlineData("http://scalio.talivio.com/download")]
    [InlineData("https://talivio.com.evil.example/x")]
    public async Task Only_talivio_com_over_https_is_accepted(string url)
    {
        var handler = new FakeDownload(Package);

        await Assert.ThrowsAsync<UpdateException>(() => Installer(handler).DownloadAsync(Status(Sha(Package), url)));

        Assert.Equal(0, handler.Requests);
    }

    /// <summary>⚠️ 302'nin VARDIĞI adres de denetlenir: yönlendirme dışarı çıktıysa durulur.</summary>
    [Fact]
    public async Task A_redirect_that_lands_off_domain_is_refused()
    {
        var handler = new FakeDownload(Package, landOn: "https://cdn.evil.example/Scalio.exe");

        await Assert.ThrowsAsync<UpdateException>(() => Installer(handler).DownloadAsync(Status(Sha(Package))));
    }

    [Fact]
    public async Task A_failed_http_status_is_reported_not_installed()
    {
        var handler = new FakeDownload(Package, status: HttpStatusCode.NotFound);

        var e = await Assert.ThrowsAsync<UpdateException>(() => Installer(handler).DownloadAsync(Status(Sha(Package))));

        Assert.Contains("404", e.Message);
    }

    /// <summary>Aynı doğrulanmış paket ikinci kez indirilmez.</summary>
    [Fact]
    public async Task An_already_verified_package_is_reused_without_a_new_download()
    {
        var handler = new FakeDownload(Package);
        var installer = Installer(handler);

        await installer.DownloadAsync(Status(Sha(Package)));
        await installer.DownloadAsync(Status(Sha(Package)));

        Assert.Equal(1, handler.Requests);
    }

    /// <summary>Kurulumdan hemen önce YENİDEN doğrulanır; arada değişmiş dosya çalıştırılmaz.</summary>
    [Fact]
    public async Task Starting_the_installer_re_verifies_the_file_first()
    {
        Directory.CreateDirectory(_root);
        var path = Path.Combine(_root, "Scalio-2.4.0-setup.exe");
        await File.WriteAllBytesAsync(path, Package);

        await Assert.ThrowsAsync<UpdateException>(() => UpdateInstaller.StartInstallerAsync(path, Sha(Encoding.ASCII.GetBytes("degisti"))));

        Assert.False(File.Exists(path)); // tutmayan dosya silindi
    }

    [Fact]
    public void CanInstall_requires_both_an_address_and_a_full_checksum()
    {
        Assert.True(Status(Sha(Package)).CanInstall);
        Assert.False(Status(null).CanInstall);
        Assert.False(Status("abc").CanInstall);
        Assert.False(new UpdateStatus("2.4.0", "2.4.0", false, false, "https://scalio.talivio.com/download", Sha(Package)).CanInstall);
    }

    /// <summary>Sunucunun özeti normalize edilir: büyük harf kabul, bozuk değer "yok".</summary>
    [Fact]
    public async Task The_checker_normalises_the_published_checksum()
    {
        var upper = Sha(Package).ToUpperInvariant();
        var handler = new FakeJson($$"""{"latest":"99.0.0","url":"https://scalio.talivio.com/download","sha256":"{{upper}}"}""");

        var status = await new UpdateChecker(new HttpClient(handler)) { UsesWindowsInstaller = true }.CheckAsync();

        Assert.Equal(Sha(Package), status.Sha256);
        Assert.True(status.CanInstall);

        var bad = await new UpdateChecker(new HttpClient(new FakeJson("""{"latest":"99.0.0","url":"https://scalio.talivio.com/download","sha256":"zz"}"""))) { UsesWindowsInstaller = true }.CheckAsync();

        Assert.Null(bad.Sha256);
        Assert.False(bad.CanInstall);
    }

    /// <summary>
    /// macOS/Linux: sunucunun adresi ve özeti Windows yükleyicisinindir — kullanıcı
    /// sitenin indirme sayfasına gider, `.exe` özeti taşınmaz.
    /// </summary>
    [Fact]
    public async Task Other_systems_are_sent_to_the_download_page_not_the_windows_installer()
    {
        var handler = new FakeJson($$"""{"latest":"99.0.0","url":"https://scalio.talivio.com/download/Scalio-99.0.0-setup.exe","sha256":"{{Sha(Package)}}"}""");

        var status = await new UpdateChecker(new HttpClient(handler)) { UsesWindowsInstaller = false }.CheckAsync();

        Assert.True(status.UpdateAvailable);
        Assert.Equal(ScalioOptions.RelayUrl, status.DownloadUrl);
        Assert.Null(status.Sha256);
        Assert.False(status.CanInstall);
    }

    private sealed class FakeJson(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
    }

    private sealed class InlineProgressList(List<(long, long)> sink) : IProgress<(long Done, long Total)>
    {
        public void Report((long Done, long Total) value) => sink.Add(value);
    }
}
