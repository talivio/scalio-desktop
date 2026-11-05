using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Scalio.Shell;

namespace Scalio.App;

public partial class MainWindow : Window
{
    /// <summary>
    /// Arayüz dosyaları bu SANAL ADRESTEN sunulur.
    ///
    /// ⚠️ Yerel bir HTTP sunucusu AÇILMAZ. Önceki sürüm gömülü bir web
    /// sunucusu çalıştırıyordu ve bağlantı noktası çakışmaları uygulamanın
    /// hiç açılmamasının sebeplerinden biriydi. WebView2'nin sanal ana
    /// bilgisayar eşlemesi aynı işi port, soket ve güvenlik duvarı uyarısı
    /// olmadan yapıyor.
    /// </summary>
    private const string UiHost = "scalio.local";

    private readonly UpscaleBridge _bridge = new(new WpfDesktopPlatform());

    public MainWindow()
    {
        InitializeComponent();

        Loaded += OnLoadedAsync;
    }

    /// <summary>İkinci kopya çalıştırıldığında var olan pencereyi öne getirir.</summary>
    public void BringToFront()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Show();
        Activate();

        /*
         * ⚠️ `Topmost` bir an açılıp kapanıyor: Windows, odağı olmayan bir
         * sürecin pencereyi öne almasını engelliyor (odak hırsızlığı koruması)
         * ve `Activate()` tek başına yalnız görev çubuğunu titretiyor.
         * Pencereyi kalıcı olarak üstte bırakmak ise saygısızlık olurdu.
         */
        Topmost = true;
        Topmost = false;

        Focus();
    }

    private async void OnLoadedAsync(object sender, RoutedEventArgs e)
    {
        /*
         * ⚠️ BURADAKİ HER İSTİSNA PENCEREYİ BOŞ BIRAKIR. `async void` olduğu
         * için istisna yukarı çıkmıyor, uygulamayı sessizce düşürüyordu — hem
         * de en olası nedenle: WebView2 çalışma zamanı kurulu değil. Bu araç
         * yeni kurulmuş, bozulmuş ya da internetsiz makinelerde açılıyor;
         * "bir şey olmadı" yerine ne olduğunu ve ne yapılacağını söylemek şart.
         */
        try
        {
            await StartBrowserAsync();
        }
        catch (Exception error)
        {
            MessageBox.Show(
                Scalio.Core.Localization.Strings.Get("app.webview_missing", error.Message),
                "Scalio",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            Application.Current.Shutdown();
        }
    }

    private async Task StartBrowserAsync()
    {
        /*
         * Kullanıcı verisi klasörü AÇIKÇA veriliyor: varsayılan konum
         * uygulamanın yanındaki dizindir ve "Program Files" altına kurulan bir
         * uygulamada oraya yazılamaz — WebView2 sessizce başlatılamaz ve
         * pencere boş kalırdı.
         */
        var userData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Scalio", "WebView2");

        Directory.CreateDirectory(userData);

        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userData);

        await Browser.EnsureCoreWebView2Async(environment);

        var core = Browser.CoreWebView2;

        /*
         * ⚠️ Arayüz İKİLİNİN İÇİNDEN sunuluyor, diskteki bir klasörden değil.
         * Yanı başında duran bir `ui/app.js`, uygulamanın davranışını
         * değiştirmek için açık bir kapı olurdu; ayrıca tek dosya taşınabilirlik
         * bu üründe ürünsel bir kazanç (kurtarılacak diske dosya yaymıyoruz).
         */
        core.AddWebResourceRequestedFilter($"https://{UiHost}/*", CoreWebView2WebResourceContext.All);
        core.WebResourceRequested += ServeEmbeddedResource;

        /*
         * ⚠️ Tarayıcı kabuğu kapatılıyor: bu bir uygulama penceresi, tarayıcı
         * değil. Sağ tık menüsü, geliştirici araçları ve yakınlaştırma
         * kısayolları kullanıcının kaybolmasına yol açar; açık kalan bir
         * "kaynağı görüntüle" de arayüzü kurcalamanın davetiyesidir.
         */
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.IsZoomControlEnabled = false;
        core.Settings.IsStatusBarEnabled = false;

        // Dış bağlantılar SİSTEM TARAYICISINDA açılır: bu pencerenin içinde
        // talivio.com açmak, kullanıcının adres çubuğunu göremediği bir yerde
        // giriş yapmasını istemek olurdu.
        core.NewWindowRequested += (_, args) =>
        {
            args.Handled = true;
            OpenExternally(args.Uri);
        };

        /*
         * ⚠️ ÜST DÜZEY GEZİNME DE KISITLANIR, yalnız yeni pencere isteği değil.
         * Yeni pencereyi engellemek, aynı sekmede açılan düz bir bağlantıyı
         * engellemez: arayüzdeki (ya da ileride oraya sızan) bir `<a href>`
         * uygulamanın kendi penceresini dış bir siteye götürebilirdi —
         * adres çubuğu olmayan, "Scalio" başlıklı bir pencerede. Kullanıcıdan
         * orada bir şeye güvenmesini istemek, kimlik avının hazır zeminidir.
         */
        core.NavigationStarting += (_, args) =>
        {
            if (args.Uri.StartsWith($"https://{UiHost}/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            args.Cancel = true;

            OpenExternally(args.Uri);
        };

        core.WebMessageReceived += OnWebMessageAsync;

        // ⚠️ Kuyruk durumu OLAY TABANLI iletilir, yoklama (polling) DEĞİL —
        // arayüz bir isteğin cevabını beklemeden, iş durumu değiştiği anda
        // haberdar olur (bkz. UpscaleBridge.PushEvent).
        _bridge.SetPushHandler(payload => Dispatcher.Invoke(() => Post(payload)));

        core.Navigate($"https://{UiHost}/index.html");
    }

    /// <summary>
    /// Arayüzden gelen istekleri karşılar.
    ///
    /// ⚠️ İŞ HER ZAMAN ARKA PLANDA. Tarama ve kurtarma saniyeler-dakikalar
    /// sürüyor; bunları arayüz iş parçacığında çalıştırmak pencereyi
    /// dondurur — kullanıcının "çok yavaş / donuyor" şikâyeti tam olarak bu
    /// sınıftaki hatanın önceki sürümdeki karşılığıydı (gömülü sunucu tarama
    /// bitene kadar hiçbir isteğe cevap veremiyordu).
    /// </summary>
    private async void OnWebMessageAsync(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        BridgeRequest? request;

        try
        {
            request = JsonSerializer.Deserialize<BridgeRequest>(e.WebMessageAsJson);
        }
        catch (JsonException)
        {
            return;
        }

        if (request is null)
        {
            return;
        }

        var response = await _bridge.HandleAsync(request);

        Post(response);
    }

    /// <summary>
    /// Gömülü arayüz dosyalarını WebView2'ye verir.
    /// </summary>
    private void ServeEmbeddedResource(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        // Arayüz Scalio.Shell'e gömülü; macOS/Linux kabuğu da aynı dosyaları sunar.
        if (UiResources.Open(new Uri(e.Request.Uri)) is not { } resource)
        {
            e.Response = Browser.CoreWebView2.Environment.CreateWebResourceResponse(
                null, 404, "Not Found", "");

            return;
        }

        e.Response = Browser.CoreWebView2.Environment.CreateWebResourceResponse(
            resource.Stream, 200, "OK", $"Content-Type: {resource.ContentType}");
    }

    private void Post(object payload)
        => Browser.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(payload, BridgeJson.Options));

    private static void OpenExternally(string url)
    {
        if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
    }
}
