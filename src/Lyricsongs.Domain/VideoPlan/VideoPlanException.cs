namespace Lyricsongs.Domain.VideoPlan;

public sealed class VideoPlanException : Exception
{
    public VideoPlanValidationResult? Validation { get; }

    public VideoPlanException(string message)
        : base(message)
    {
    }

    public VideoPlanException(string message, VideoPlanValidationResult validation)
        : base(message)
    {
        Validation = validation;
    }

    public VideoPlanException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
