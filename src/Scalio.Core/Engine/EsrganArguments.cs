using System.Globalization;

namespace Scalio.Core.Engine;

/// <summary>`realesrgan-ncnn-vulkan.exe` komut satırını kuran ortak yardımcı.</summary>
internal static class EsrganArguments
{
    public static string Build(
        string inputPath,
        string outputPath,
        string modelName,
        string modelsPath,
        int scale,
        int? tileSize)
    {
        var arguments = string.Create(
            CultureInfo.InvariantCulture,
            $"-i \"{inputPath}\" -o \"{outputPath}\" -n {modelName} -m \"{modelsPath}\" -s {scale}");

        if (tileSize is { } tile)
        {
            arguments += string.Create(CultureInfo.InvariantCulture, $" -t {tile}");
        }

        return arguments;
    }
}
