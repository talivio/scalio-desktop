using System.Windows;
using System.Windows.Threading;
using Scalio.Core.Diagnostics;

namespace Scalio.App;

public partial class App : Application
{
    private SingleInstance? _instance;

    /// <summary>
    /// Günlük satırlarının kaynağı: "app" ya da "elevated".
    /// </summary>
    /// <remarks>
    /// ⚠️ Kapanış satırı bir süre SABİT "app" yazıyordu ve yükseltilmiş
    /// sürecin kapanışı da öyle görünüyordu. İki süreç aynı dosyaya yazdığı
    /// için bu, teşhis sırasında yanlış süreci takip ettiriyordu.
    /// </remarks>
    private string _role = "app";

    /// <summary>Bu açılış, uygulama içi güncellemenin hemen ardından mı (`--updated`)?</summary>
    public static bool JustUpdated { get; private set; }

    /// <summary>
    /// Uygulama içi güncelleme için: tek-kopya kilidini bırakır ve kapanır.
    /// </summary>
    /// <remarks>
    /// ⚠️ SIRA ÖNEMLİ. Kurulum programı aynı mutex'e bakıyor (scalio.iss →
    /// AppMutex); kilit süreçle birlikte ölseydi de olurdu ama kapanış
    /// WebView2'nin boşalmasını bekliyor ve bu bir-iki saniye sürebiliyor —
    /// kurulum o sırada "Scalio çalışıyor" deyip durabilirdi. Kilit ÖNCE
    /// bırakılır, sonra kapanılır.
    /// </remarks>
    public void ExitForUpdate()
    {
        ScalioLog.Info(_role, "Güncelleme için kapanıyor: tek-kopya kilidi bırakıldı.");

        _instance?.Dispose();
        _instance = null;

        Dispatcher.BeginInvoke(Shutdown);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        JustUpdated = e.Args.Contains("--updated");

        /*
         * ⚠️ GÜNLÜK HER ŞEYDEN ÖNCE KURULUR — aşağıdaki dallar (kendini
         * sınama, yükseltilmiş iş, ikinci kopya) süreci ERKENDEN kapatıyor
         * ve o dallarda olan biteni de görebilmek gerekiyor.
         *
         * Rol ayrımı şart: yükseltilmiş yardımcı AYRI bir süreçtir ve aynı
         * dosyaya yazar; hangi satırın hangi süreçten geldiği ayrılmazsa
         * asıl teşhis edilmek istenen zincir okunamaz hâle gelir.
         */
        _role = e.Args.Contains("--elevated-job") ? "elevated" : "app";

        ScalioLog.Start(_role);

        HookCrashReporting();

        /*
         * ⚠️ `--selftest` KALICI BİR TEŞHİS ARACI, geliştirme artığı değil.
         *
         * Arayüz bir webview içinde çalıştığı için "uygulama açıldı ama
         * çalışmıyor" durumunda dışarıdan bakan hiç kimse sebebi göremez —
         * kullanıcının bildirdiği kusur tam olarak buydu. Bu anahtar köprüyü
         * pencere hiç açılmadan çalıştırır ve sonucu standart çıktıya yazar:
         * destek isteyen kullanıcıdan tek satırlık somut bir çıktı istemek,
         * "bende açılmıyor" cümlesiyle uğraşmaktan iyidir.
         *
         * Pencere açılmadığı için tek kopya kilidi de burada KURULMAZ:
         * kendini sınama, açık duran uygulamayı etkilemeden çalışabilmeli.
         */
        if (e.Args.Contains("--selftest"))
        {
            SelfTest.Run();

            Shutdown();

            return;
        }

        // NOT: Salvio'daki "yükseltilmiş iş" (UAC ile yeniden başlatma) burada
        // yok — Scalio yönetici yetkisi istemiyor, `Scalio.Core.Elevation`
        // kopyalanmadı.

        _instance = SingleInstance.Acquire();

        /*
         * ⚠️ İKİNCİ KOPYA SESSİZCE ÖLMEZ: var olan pencereyi öne getirmesini
         * söyleyip kapanır. Sessiz kapanış, kullanıcı açısından "çift
         * tıkladım, hiçbir şey olmadı" demektir — düzeltmeye çalıştığımız
         * kusurun ta kendisi.
         */
        if (!_instance.IsFirstInstance)
        {
            SingleInstance.SignalExistingInstance();

            Shutdown();

            return;
        }

        base.OnStartup(e);

        var window = new MainWindow();
        MainWindow = window;

        _instance.ListenForActivation(() => Dispatcher.Invoke(window.BringToFront));

        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ScalioLog.Info(_role, $"Kapanıyor (çıkış kodu {e.ApplicationExitCode}).");

        _instance?.Dispose();

        base.OnExit(e);
    }

    /// <summary>
    /// Yakalanmamış her hatayı günlüğe düşürür.
    /// </summary>
    /// <remarks>
    /// ⚠️ BUNSUZ ÇÖKMELER İZ BIRAKMIYORDU. Bir GUI uygulamasının konsolu
    /// yoktur; arayüz iş parçacığında yakalanmayan bir istisna pencereyi
    /// kapatıyor ve geriye Windows Olay Görüntüleyicisi'nde okunması destekten
    /// beklenemeyecek bir kayıttan başka hiçbir şey kalmıyordu.
    ///
    /// ⚠️ `DispatcherUnhandledException` İŞLENMİŞ SAYILMAZ (`Handled = true`
    /// yapılmıyor): hatayı yutup uygulamayı ayakta tutmak, tutarsız duruma
    /// düşmüş bir kurtarma aracını çalışmaya devam ettirmek olurdu. Amaç
    /// çökmeyi engellemek değil, çökmeyi GÖRÜNÜR kılmak.
    /// </remarks>
    private void HookCrashReporting()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            ScalioLog.Error("crash", "Yakalanmamış hata", args.ExceptionObject as Exception);

        DispatcherUnhandledException += (_, args) =>
            ScalioLog.Error("crash", "Arayüz iş parçacığında yakalanmamış hata", args.Exception);

        /*
         * ⚠️ Gözlemlenmemiş görev hataları da yazılır. Köprüdeki `async`
         * yollar `await` edilmediğinde hata sessizce yutuluyor ve kullanıcı
         * yalnız "bir şey olmadı" görüyordu.
         */
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            ScalioLog.Error("crash", "Gözlemlenmemiş görev hatası", args.Exception);

            args.SetObserved();
        };
    }
}
