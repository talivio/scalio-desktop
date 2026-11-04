namespace Scalio.Core.Engine;

/// <summary>Motor (Real-ESRGAN) çalıştırmasıyla ilgili tüm hataların ortak temeli.</summary>
public abstract class EngineException : Exception
{
    protected EngineException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Motor belleği yetmediği için başarısız oldu (tile küçültme denemesi de tükendi).
/// </summary>
public sealed class EngineOutOfMemoryException : EngineException
{
    public EngineOutOfMemoryException(string message)
        : base(message)
    {
    }
}

/// <summary>Motor OOM dışında bir sebeple başarısız oldu (eksik dosya, geçersiz model, çökme...).</summary>
public sealed class EngineFailedException : EngineException
{
    public EngineFailedException(string message)
        : base(message)
    {
    }
}
