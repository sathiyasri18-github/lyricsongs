using Lyricsongs.Domain.Lyrics;

namespace Lyricsongs.Infrastructure.Lyrics.Ass;

internal static class AssLayoutHelper
{
    /// <summary>
    /// ASS numpad alignment (1–9): bottom-left=1, bottom-center=2, bottom-right=3, etc.
    /// </summary>
    public static int GetAssAlignment(LyricStyleConfiguration config) =>
        (config.Position, config.Alignment) switch
        {
            (LyricPosition.Bottom, LyricTextAlignment.Left) => 1,
            (LyricPosition.Bottom, LyricTextAlignment.Center) => 2,
            (LyricPosition.Bottom, LyricTextAlignment.Right) => 3,
            (LyricPosition.AboveBottom, LyricTextAlignment.Left) => 1,
            (LyricPosition.AboveBottom, LyricTextAlignment.Center) => 2,
            (LyricPosition.AboveBottom, LyricTextAlignment.Right) => 3,
            (LyricPosition.Center, LyricTextAlignment.Left) => 4,
            (LyricPosition.Center, LyricTextAlignment.Center) => 5,
            (LyricPosition.Center, LyricTextAlignment.Right) => 6,
            (LyricPosition.Top, LyricTextAlignment.Left) => 7,
            (LyricPosition.Top, LyricTextAlignment.Center) => 8,
            (LyricPosition.Top, LyricTextAlignment.Right) => 9,
            (LyricPosition.BelowTop, LyricTextAlignment.Left) => 7,
            (LyricPosition.BelowTop, LyricTextAlignment.Center) => 8,
            (LyricPosition.BelowTop, LyricTextAlignment.Right) => 9,
            _ => 2
        };

    public static (int X, int Y) GetAnchorPoint(LyricStyleConfiguration config)
    {
        var x = config.Alignment switch
        {
            LyricTextAlignment.Left => config.MarginHorizontal,
            LyricTextAlignment.Right => config.VideoWidth - config.MarginHorizontal,
            _ => config.VideoWidth / 2
        };

        var y = config.Position switch
        {
            LyricPosition.Top => config.MarginVertical,
            LyricPosition.BelowTop => config.MarginVertical + (int)(config.VideoHeight * 0.12),
            LyricPosition.Center => config.VideoHeight / 2,
            LyricPosition.AboveBottom => config.VideoHeight - config.MarginVertical - (int)(config.VideoHeight * 0.12),
            _ => config.VideoHeight - config.MarginVertical
        };

        return (x, y);
    }
}
