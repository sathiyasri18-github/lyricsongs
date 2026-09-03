namespace Lyricsongs.Domain.Rendering;

public sealed class RenderOutputConfiguration
{
    public int Width { get; init; } = 1920;
    public int Height { get; init; } = 1080;
    public int FrameRate { get; init; } = 30;
    public string VideoCodec { get; init; } = "libx264";
    public int VideoCrf { get; init; } = 18;
    public string VideoPreset { get; init; } = "medium";
    public string AudioCodec { get; init; } = "aac";
    public int AudioBitrateKbps { get; init; } = 192;
    public string OutputFormat { get; init; } = "mp4";
    public KenBurnsMotion DefaultKenBurnsMotion { get; init; } = KenBurnsMotion.ZoomIn;
    public bool CycleKenBurnsMotions { get; init; } = true;
    public string XfadeTransition { get; init; } = "fade";
}
