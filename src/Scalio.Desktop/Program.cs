using System.Text.Json;
using Photino.NET;
using Scalio.Core.Diagnostics;
using Scalio.Shell;

namespace Scalio.Desktop;

/// <summary>
/// macOS / Linux kabuğu. Windows'taki MainWindow'un karşılığı; arayüz ve
/// köprü aynı (Scalio.Shell), yalnız pencere ve webview bağlantısı farklı.
/// </summary>
public static class Program
{
    /// <summary>
    /// Arayüz bu ÖZEL ŞEMADAN sunulur (`app://scalio/index.html`).
    ///
    /// ⚠️ Yerel bir HTTP sunucusu AÇILMAZ — Windows'taki WebView2 sanal adres
    /// eşlemesinin karşılığı: dosyalar ikiliden doğrudan akışla verilir; port,
    /// soket ve güvenlik duvarı uyarısı yok.
    /// </summary>
    private const string UiScheme = "app";

    private static PhotinoWindow? _window;
    private static UpscaleBridge? _bridge;

    [STAThread]
    public static int Main(string[] args)
    {
        ScalioLog.Start("app");

        HookCrashReporting();

        // ⚠️ `--selftest` pencere açmadan motoru GERÇEKTEN çalıştırır — destekte
        // ve CI'da (ekransız Linux) aynı komut kullanılıyor.
        if (args.Contains("--selftest"))
        {
            return DesktopSelfTest.Run();
        }

        _window = new PhotinoWindow()
            .SetTitle("Scalio")
            .SetUseOsDefaultSize(false)
            .SetSize(1120, 760)
            .SetMinSize(880, 560)
            .Center()
            .SetDevToolsEnabled(Environment.GetEnvironmentVariable("SCALIO_DEVTOOLS") == "1")
            .SetContextMenuEnabled(false)
            .RegisterCustomSchemeHandler(UiScheme, ServeEmbeddedResource)
            .RegisterWebMessageReceivedHandler(OnWebMessage);

        // macOS'ta Dock simgesi paketin .icns'inden gelir; Linux'ta pencereye verilir.
        var icon = Path.Combine(AppContext.BaseDirectory, "scalio.png");

        if (OperatingSystem.IsLinux() && File.Exists(icon))
        {
            _window.SetIconFile(icon);
        }

        _bridge = new UpscaleBridge(new PhotinoDesktopPlatform(_window));

        // ⚠️ Kuyruk durumu OLAY TABANLI iletilir, yoklama değil (bkz. UpscaleBridge.PushEvent).
        _bridge.SetPushHandler(Post);

        _window.Load(new Uri($"{UiScheme}://scalio/index.html"));
        _window.WaitForClose();

        ScalioLog.Info("app", "Kapanıyor.");

        return 0;
    }

    private static Stream ServeEmbeddedResource(object sender, string scheme, string url, out string contentType)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && UiResources.Open(uri) is { } resource)
        {
            contentType = resource.ContentType;

            return resource.Stream;
        }

        contentType = "text/plain";

        return new MemoryStream();
    }

    /// <summary>
    /// Arayüzden gelen istekleri karşılar.
    ///
    /// ⚠️ Cevap BEKLENMEDEN döner: köprü işi arka planda yapar, cevap hazır
    /// olunca gönderilir. Arayüz iş parçacığında beklemek pencereyi dondururdu.
    /// </summary>
    private static void OnWebMessage(object? sender, string message)
    {
        BridgeRequest? request;

        try
        {
            request = JsonSerializer.Deserialize<BridgeRequest>(message);
        }
        catch (JsonException)
        {
            return;
        }

        if (request is null || _bridge is null)
        {
            return;
        }

        _ = RespondAsync(_bridge, request);
    }

    private static async Task RespondAsync(UpscaleBridge bridge, BridgeRequest request)
    {
        var response = await bridge.HandleAsync(request);

        Post(response);
    }

    /// <summary>
    /// ⚠️ Gönderim HER ZAMAN pencerenin iş parçacığından: köprü olayları ve
    /// cevaplar arka plan iş parçacıklarından geliyor; WKWebView/WebKitGTK'ya
    /// başka bir iş parçacığından dokunmak çökme sebebidir.
    /// </summary>
    private static void Post(object payload)
    {
        var window = _window;

        if (window is null)
        {
            return;
        }

        var json = JsonSerializer.Serialize(payload, BridgeJson.Options);

        window.Invoke(() => window.SendWebMessage(json));
    }

    /// <summary>Yakalanmamış her hatayı günlüğe düşürür (Windows kabuğundakiyle aynı ilke).</summary>
    private static void HookCrashReporting()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            ScalioLog.Error("crash", "Yakalanmamış hata", args.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            ScalioLog.Error("crash", "Gözlemlenmemiş görev hatası", args.Exception);

            args.SetObserved();
        };
    }
}
