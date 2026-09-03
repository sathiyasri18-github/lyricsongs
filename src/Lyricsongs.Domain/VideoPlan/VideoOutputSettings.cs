namespace Lyricsongs.Domain.VideoPlan;

public sealed class VideoOutputSettings
{
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int Fps { get; set; } = 30;
    public string VideoCodec { get; set; } = "libx264";
    public string AudioCodec { get; set; } = "aac";
    public string Format { get; set; } = "mp4";
    public VideoAspectRatio AspectRatio { get; set; } = VideoAspectRatio.Landscape16x9;
    public VideoResolutionPreset ResolutionPreset { get; set; } = VideoResolutionPreset.FullHd1080p;
}
