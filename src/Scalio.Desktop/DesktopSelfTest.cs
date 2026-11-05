using Scalio.Core;
using Scalio.Core.Engine;
using Scalio.Shell;

namespace Scalio.Desktop;

/// <summary>
/// `Scalio --selftest` — pencere açmadan zincirin her halkasını yazdırır.
///
/// ⚠️ Windows'takinden farkı: motoru GERÇEKTEN çalıştırır (GpuProbe, 8×8
/// görsel). macOS'ta MoltenVK, Linux'ta Vulkan sürücüsü — "uygulama açıldı ama
/// büyütmüyor" şikâyetinin en olası sebebi tam bu halka.
///
/// Çıkış kodu: 0 her şey tamam, 1 bir halka eksik. CI bu koda bakar.
/// </summary>
public static class DesktopSelfTest
{
    public static int Run()
    {
        var ok = true;

        Line("Scalio kendini sınama");
        Line(new string('─', 44));

        ok &= Check("Arayüz kaynakları", () =>
        {
            using var entry = UiResources.Assembly.GetManifestResourceStream("ui/index.html");

            return entry is null ? (false, "EKSİK: ui/index.html") : (true, "gömülü, tamam");
        });

        ok &= Check("Motor", () => File.Exists(ScalioOptions.EngineExecutable)
            ? (true, ScalioOptions.EngineExecutable)
            : (false, "BULUNAMADI: " + ScalioOptions.EngineExecutable));

        ok &= Check("Modeller", () => File.Exists(Path.Combine(ScalioOptions.ModelsPath, "realesrgan-x4plus.param"))
            ? (true, ScalioOptions.ModelsPath)
            : (false, "BULUNAMADI: " + ScalioOptions.ModelsPath));

        ok &= Check("GPU / Vulkan (gerçek çalıştırma)", () =>
        {
            var result = new GpuProbe()
                .ProbeAsync(ScalioOptions.EngineExecutable, ScalioOptions.ModelsPath)
                .GetAwaiter().GetResult();

            return result.Availability == GpuAvailability.Available
                ? (true, "çalışıyor")
                : (false, result.Message ?? "çalışmıyor");
        });

        Line(new string('─', 44));
        Line(ok ? "Bitti: tamam." : "Bitti: SORUN VAR.");

        return ok ? 0 : 1;
    }

    private static bool Check(string name, Func<(bool Ok, string Text)> probe)
    {
        try
        {
            var (ok, text) = probe();

            Line($"{name,-34} {text}");

            return ok;
        }
        catch (Exception e)
        {
            // Tek bir sınamanın patlaması diğerlerini DURDURMAZ.
            Line($"{name,-34} HATA: {e.Message}");

            return false;
        }
    }

    private static void Line(string text)
    {
        Console.WriteLine(text);
        Console.Out.Flush();
    }
}
