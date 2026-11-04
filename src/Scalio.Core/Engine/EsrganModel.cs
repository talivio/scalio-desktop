namespace Scalio.Core.Engine;

/// <summary>Real-ESRGAN'ın desteklediği iki genel amaçlı model.</summary>
public enum EsrganModel
{
    Photo,
    Anime,
}

public static class EsrganModelExtensions
{
    public static string ToModelName(this EsrganModel model) => model switch
    {
        EsrganModel.Photo => "realesrgan-x4plus",
        EsrganModel.Anime => "realesrgan-x4plus-anime",
        _ => throw new ArgumentOutOfRangeException(nameof(model), model, null),
    };
}
