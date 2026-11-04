using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace Scalio.App;

/// <summary>
/// Uygulamanın tek kopya çalışmasını sağlar ve ikinci çalıştırmada var olan
/// pencereyi öne getirir.
///
/// ⚠️ BU SINIF, KULLANICININ BİR NUMARALI ŞİKÂYETİNİN KARŞILIĞIDIR.
/// Önceki (NativePHP/Electron) sürümde uygulama zaten açıkken simgeye ikinci
/// kez tıklandığında yeni bir süreç doğuyor, o süreç ilkinin tuttuğu bağlantı
/// noktasını alamadığı için PENCERE HİÇ AÇILMIYOR ve sessizce ölüyordu.
/// Kullanıcı "çift tıkladım, hiçbir şey olmadı" diyordu ve haklıydı.
/// Electron'un `requestSingleInstanceLock` çağrısı NativePHP'de hiç
/// kurulmamıştı; düzeltme `vendor/` içinde kaldığı için de yapılamıyordu.
///
/// ⚠️ Mutex TEK BAŞINA YETMEZ: ikinci kopyanın sessizce kapanması,
/// kullanıcı açısından yine "hiçbir şey olmadı" demektir. Bu yüzden ikinci
/// kopya, kapanmadan önce adlandırılmış kanal üzerinden ilk kopyaya
/// "kendini göster" der.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Global\Scalio.SingleInstance";

    /// <summary>Oturuma özel yedek ad — `Global\` açılamadığında kullanılır.</summary>
    private const string LocalMutexName = @"Local\Scalio.SingleInstance";

    private const string PipeName = "Scalio.Activate";

    private readonly Mutex _mutex;
    private CancellationTokenSource? _listener;

    private SingleInstance(Mutex mutex, bool isFirst)
    {
        _mutex = mutex;
        IsFirstInstance = isFirst;
    }

    public bool IsFirstInstance { get; }

    public static SingleInstance Acquire()
    {
        /*
         * `Global\` öneki oturumlar arası: hızlı kullanıcı değiştirme ya da
         * uzak masaüstü oturumlarında da tek kopya kalır. Mutex'in sahipliği
         * BAŞLANGIÇTA istenmiyor (`initiallyOwned: false`) — sahiplik
         * `createdNew` bayrağıyla belirleniyor ve terk edilmiş mutex
         * istisnasından kaçınılıyor.
         */
        try
        {
            var mutex = new Mutex(initiallyOwned: false, MutexName, out var createdNew);

            return new SingleInstance(mutex, createdNew);
        }
        catch (UnauthorizedAccessException)
        {
            /*
             * ⚠️ BURASI UYGULAMAYI ÇÖKERTİYORDU. `Global\` adlı bir mutex BAŞKA
             * BİR OTURUMDA (hızlı kullanıcı değiştirme, uzak masaüstü, aynı
             * makinede ikinci bir kullanıcı) zaten varsa, bizim erişim
             * hakkımız olmayabilir ve `new Mutex(...)` istisna fırlatır. O
             * istisna açılışın en başında, hiçbir pencere açılmadan
             * gerçekleşiyordu: kullanıcı çift tıklıyor ve gerçekten HİÇBİR ŞEY
             * olmuyordu — düzeltmek için bu sınıfın yazıldığı kusurun ta
             * kendisi.
             *
             * Mutex açılamıyorsa başka biri onu tutuyor demektir; bu kopya
             * ikinci kopya gibi davranır (pencereyi öne getirmeyi dener,
             * kanal da başka oturumdaysa sessizce kendi penceresini açar).
             */
            try
            {
                var existing = Mutex.OpenExisting(MutexName);

                return new SingleInstance(existing, isFirst: false);
            }
            catch (Exception e) when (e is WaitHandleCannotBeOpenedException or UnauthorizedAccessException)
            {
                /*
                 * Ne oluşturulabiliyor ne açılabiliyor: tek kopya güvencesinden
                 * VAZGEÇİLİR, uygulama yine de açılır. Bir kolaylık uğruna
                 * programı hiç başlatmamak kabul edilemez.
                 */
                return new SingleInstance(new Mutex(initiallyOwned: false, LocalMutexName, out var createdLocal), createdLocal);
            }
        }
    }

    /// <summary>
    /// İlk kopya: ikinci çalıştırmaların "kendini göster" isteğini dinler.
    /// </summary>
    public void ListenForActivation(Action activate)
    {
        _listener = new CancellationTokenSource();
        var token = _listener.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(
                        PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(token);

                    activate();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (IOException)
                {
                    /*
                     * Kanal hatası uygulamayı DÜŞÜRMEZ: bu yol yalnızca bir
                     * kolaylık. Dinleyici ölse bile uygulamanın kendisi
                     * çalışmaya devam etmeli — aksi hâlde "pencere gelmiyor"
                     * kusurunu çözerken "uygulama çöküyor" kusurunu üretirdik.
                     */
                }
            }
        }, token);
    }

    /// <summary>
    /// İkinci kopya: ilk kopyaya kendini göstermesini söyler.
    /// </summary>
    /// <returns>Haber ulaştıysa true.</returns>
    public static bool SignalExistingInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);

            // Kısa zaman aşımı: ilk kopya yanıt vermiyorsa kullanıcıyı
            // bekletmenin anlamı yok, yine de yeni pencere açmayacağız.
            client.Connect(2000);

            return true;
        }
        catch (Exception e) when (e is TimeoutException or IOException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        _listener?.Cancel();
        _listener?.Dispose();

        _mutex.Dispose();
    }
}
