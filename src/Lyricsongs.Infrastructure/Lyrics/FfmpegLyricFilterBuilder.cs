using Lyricsongs.Domain.Lyrics;

namespace Lyricsongs.Infrastructure.Lyrics;

internal static class FfmpegLyricFilterBuilder
{
    public static LyricFfmpegFilterPlan Build(LyricStyleConfiguration config, LyricAnimationStyle style)
    {
        var assFile = "lyrics.ass";
        var assFilter = $"ass='{assFile}'";
        var styleName = style.ToString().ToLowerInvariant();

        var steps = new List<string>
        {
            "[0:v]scale=1920:1080:force_original_aspect_ratio=decrease,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,setsar=1[base]",
            $"[base]{assFilter}[vout]"
        };

        var description =
            $"Lyric style '{styleName}' is rendered via ASS subtitles (Unicode-safe for Tamil/English). " +
            $"The generated {assFile} encodes {GetStyleDescription(style)}. " +
            "FFmpeg burns subtitles with the ass filter: [base]ass='lyrics.ass'[vout]. " +
            $"Font='{config.Font}', size={config.FontSize}, position={config.Position}, alignment={config.Alignment}, " +
            $"primary={config.TextColor}, highlight={config.HighlightColor}, opacity={config.Opacity:F2}.";

        return new LyricFfmpegFilterPlan
        {
            SubtitleFilter = assFilter,
            FilterSteps = steps,
            Description = description,
            AssFileName = assFile
        };
    }

    private static string GetStyleDescription(LyricAnimationStyle style) => style switch
    {
        LyricAnimationStyle.Classic => "fade-in, hold, fade-out tags (\\fad)",
        LyricAnimationStyle.Karaoke => "progressive fill karaoke tags (\\kf) with highlight colour",
        LyricAnimationStyle.Pop => "scale transform (\\fscx/\\fscy + \\t) with fade-in",
        LyricAnimationStyle.Cinematic => "vertical move (\\move) with fade-in and fade-out",
        LyricAnimationStyle.Minimal => "simple fade-in/fade-out (\\fad)",
        _ => "style-specific ASS override tags"
    };
}
