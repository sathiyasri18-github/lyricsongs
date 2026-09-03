namespace Lyricsongs.Domain.VideoPlan;

public sealed class TransitionSettings
{
    public TransitionType Type { get; set; } = TransitionType.Fade;
    public long DurationMs { get; set; } = 500;

    public static TransitionSettings None => new()
    {
        Type = TransitionType.None,
        DurationMs = 0
    };
}
