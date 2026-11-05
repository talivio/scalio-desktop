using System.Diagnostics;
using System.Reflection;

namespace Scalio.Core;

/// <summary>Çalışan Scalio'nun sürümü — günlük, destek paketi ve sürüm denetimi aynı değeri kullanır.</summary>
public static class ScalioVersion
{
    private static string? _cached;

    /// <summary>"0.3.0" biçiminde; okunamazsa "0.0.0".</summary>
    public static string Current => _cached ??= Read();

    private static string Read()
    {
        /*
         * ⚠️ macOS ve Linux'ta `FileVersionInfo` İŞE YARAMAZ: sürüm kaynağı
         * bir PE (Windows) kavramı ve tek dosya yayında çalışan ikili Mach-O/ELF
         * uygulama sunucusudur — sonuç hep "0.0.0" olur ve her açılışta "yeni
         * sürüm var" denirdi. Orada derlemenin kendi sürüm özniteliği okunur.
         */
        if (!OperatingSystem.IsWindows())
        {
            var informational = (Assembly.GetEntryAssembly() ?? typeof(ScalioVersion).Assembly)
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            return informational?.Split('+')[0] ?? "0.0.0";
        }

        try
        {
            return FileVersionInfo
                .GetVersionInfo(Environment.ProcessPath ?? typeof(ScalioVersion).Assembly.Location)
                .ProductVersion?.Split('+')[0] ?? "0.0.0";
        }
        catch
        {
            return "0.0.0";
        }
    }
}
