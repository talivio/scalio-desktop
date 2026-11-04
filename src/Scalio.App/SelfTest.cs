using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Scalio.App.Bridge;

namespace Scalio.App;

/// <summary>
/// `Scalio.exe --selftest` — pencere açmadan temel yetenekleri sınar.
///
/// ⚠️ Neden var: arayüz bir webview içinde çalışıyor ve "açıldı ama bir şey
/// görünmüyor" durumunda dışarıdan bakan kimse sebebi göremez. Bu komut,
/// zincirin her halkasını ayrı ayrı yazdırır; destek isteyen kullanıcıdan
/// somut bir çıktı istemek, "bende çalışmıyor" cümlesiyle uğraşmaktan iyidir.
/// </summary>
public static partial class SelfTest
{
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsole(uint processId);

    /// <summary>
    /// ⚠️ TÜM SINAMA HAVUZ İŞ PARÇACIĞINDA ÇALIŞIR VE BU ZORUNLU.
    ///
    /// İlk sürüm doğrudan arayüz iş parçacığında `.Result` ile bekliyordu ve
    /// UYGULAMA KİLİTLENİYORDU: köprünün `scan` yolu `await Task.Run(...)`
    /// yapıyor, devamı WPF'in dağıtıcı bağlamına geri dönmek istiyor, ama o
    /// iş parçacığı `.Result` ile bloke — klasik async kilitlenmesi. `Task.Run`
    /// içinde yakalanmış bir arayüz bağlamı olmadığı için devam havuzda
    /// çalışır ve kilit çözülür.
    ///
    /// Bu, teşhis aracının kendisinin teşhis ettiği ilk kusur oldu.
    /// </summary>
    public static void Run()
    {
        // Bir GUI uygulamasının konsolu yoktur; çağıran kabuğunkine bağlanmak
        // çıktının görünmesini sağlar (bağlanamazsa sessizce devam eder).
        AttachConsole(0xFFFFFFFF);

        /*
         * ⚠️ Konsol kod sayfası UTF-8'e çekilir. Windows konsolu varsayılan
         * olarak 437/857 gibi eski bir kod sayfası kullanıyor ve Türkçe
         * harfler bozuk çıkıyor ("s?nama", "Aray?z"). Kullanıcıdan destek
         * için istenen bir çıktının okunamaz olması, aracın amacını
         * boşa çıkarır. Ölçüldü, tahmin değil.
         */
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }
        catch (IOException)
        {
            // Konsol yoksa (çift tıkla açıldıysa) sorun değil.
        }

        Task.Run(RunChecks).GetAwaiter().GetResult();
    }

    private static void RunChecks()
    {
        Line("Scalio kendini sınama");
        Line(new string('─', 44));

        Check("Arayüz kaynakları", () =>
        {
            // Arayüz artık ikiliye GÖMÜLÜ (bkz. Scalio.App.csproj); diskte
            // aranacak bir klasör yok.
            var assembly = typeof(SelfTest).Assembly;

            using var entry = assembly.GetManifestResourceStream("ui/index.html");

            if (entry is null)
            {
                return "EKSİK: ui/index.html";
            }

            /*
             * ⚠️ BEKLENEN DOSYA LİSTESİ ELLE YAZILMAZ, HTML'DEN TÜRETİLİR.
             *
             * Önceki sürümde liste sabitti (`index.html`, `app.css`, `app.js`)
             * ve arayüze yeni bir betik eklendiğinde denetimin dışında
             * kalıyordu: dosya pakete girmese bile bu satır "tamam" diyecek,
             * uygulama açılacak ve yalnızca o betiğin yaptığı iş sessizce
             * kaybolacaktı. Teşhis aracının kendisi, teşhis etmesi gereken
             * kusuru gizler hâle gelirdi.
             */
            var html = new StreamReader(entry).ReadToEnd();

            var referenced = System.Text.RegularExpressions.Regex
                .Matches(html, @"(?:src|href)=""(?<file>[A-Za-z0-9._-]+\.(?:js|css))""")
                .Select(match => "ui/" + match.Groups["file"].Value)
                .Distinct()
                .ToArray();

            var missing = referenced
                .Where(name => assembly.GetManifestResourceStream(name) is null)
                .ToArray();

            return missing.Length == 0
                ? $"gömülü, tamam ({referenced.Length + 1} dosya)"
                : "EKSİK: " + string.Join(", ", missing);
        });

        Check("WebView2 çalışma zamanı", () =>
        {
            /*
             * ⚠️ En olası "pencere boş" sebebi bu: WebView2 Evergreen
             * çalışma zamanı kurulu değilse pencere açılır ama içi hiç
             * çizilmez. Sürümü yazdırmak, o durumu bir bakışta ayırır.
             */
            var version = CoreWebView2Environment.GetAvailableBrowserVersionString();

            return string.IsNullOrEmpty(version) ? "KURULU DEĞİL" : version;
        });

        // TODO: motor entegrasyonu (Real-ESRGAN) sonraki adımda gelecek —
        // o zaman buraya Scalio'ya özel sınamalar eklenecek (ör. çalıştırılabilir
        // bulundu mu, model dosyaları yerinde mi).

        Line(new string('─', 44));
        Line("Bitti.");
    }

    private static void Check(string name, Func<string> probe)
    {
        try
        {
            Line($"{name,-30} {probe()}");
        }
        catch (Exception e)
        {
            // Tek bir sınamanın patlaması diğerlerini DURDURMAZ: teşhis
            // aracının kendisi yarıda kesilirse hiçbir işe yaramaz.
            Line($"{name,-30} HATA: {e.Message}");
        }
    }

    private static void Line(string text)
    {
        Console.WriteLine(text);
        Console.Out.Flush();
    }
}
