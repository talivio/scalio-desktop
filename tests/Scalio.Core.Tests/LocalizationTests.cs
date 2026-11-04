using System.Text.RegularExpressions;
using Scalio.Core.Localization;
using Scalio.Core.Tests.Support;

namespace Scalio.Core.Tests;

/// <summary>
/// Çeviri sözlüğünün kapsamı.
///
/// ⚠️ NEDEN TEST EDİLİYOR: `Strings.Get` tanımsız bir anahtarda istisna
/// fırlatmaz, ANAHTARIN KENDİSİNİ döndürür. Yani bir yazım hatası derlemeyi de
/// testleri de geçer ve kullanıcı hata anında ekranda "recover.no_destination"
/// yazısını görür. Sessiz bozulmanın ders kitabı örneği.
/// </summary>
[Collection(LanguageCollection.Name)]
public sealed class LocalizationTests
{
    /// <summary>Depodaki kaynak kökü (test ikilisinden yukarı çıkarak bulunur).</summary>
    private static string SourceRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src", "Scalio.Core")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);

        return Path.Combine(directory!.FullName, "src");
    }

    /// <summary>Kodda geçen bütün `Strings.Get("…")` anahtarları.</summary>
    private static IEnumerable<(string Key, string File)> UsedKeys()
    {
        foreach (var file in Directory.EnumerateFiles(SourceRoot(), "*.cs", SearchOption.AllDirectories))
        {
            // Derleme çıktısı taranmaz: aynı kaynağın kopyalarını sayardı.
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            foreach (Match match in Regex.Matches(File.ReadAllText(file), """Strings\.Get\("([a-z0-9_.]+)"""))
            {
                yield return (match.Groups[1].Value, Path.GetFileName(file));
            }
        }
    }

    /// <summary>
    /// ⚠️ Kullanılan her anahtar TANIMLI olmalı. Aksi hâlde kullanıcı, tam da
    /// bir şeyin ters gittiği anda cümle yerine anahtar adı görür.
    /// </summary>
    [Fact]
    public void Every_key_used_in_code_exists_in_the_dictionary()
    {
        var missing = UsedKeys()
            .Where(entry => Strings.Get(entry.Key) == entry.Key)
            .Select(entry => $"{entry.Key} ({entry.File})")
            .Distinct()
            .Order()
            .ToList();

        Assert.True(missing.Count == 0, "Sözlükte karşılığı olmayan anahtarlar: " + string.Join(", ", missing));
    }

    /// <summary>
    /// ⚠️ Her anahtar ÜÇ DİLDE de olmalı. Eksik çeviride İngilizceye düşülüyor
    /// ama bu sessiz bir gerileme: Estonca arayüzdeki kullanıcı, hata anında
    /// karışık dilli bir ekranla karşılaşır ve PHP sürümünde bu gerçekten
    /// yaşandı.
    /// </summary>
    [Fact]
    public void Every_key_is_translated_into_all_three_languages()
    {
        var keys = UsedKeys().Select(entry => entry.Key).Distinct().ToList();

        Assert.NotEmpty(keys);

        var gaps = new List<string>();

        foreach (var language in Strings.Supported)
        {
            using var scope = new LanguageScope(language);

            gaps.AddRange(keys.Where(key => Strings.Get(key) == key).Select(key => $"{key}/{language}"));
        }

        Assert.True(gaps.Count == 0, "Çevirisi eksik: " + string.Join(", ", gaps.Distinct().Order()));
    }
}
