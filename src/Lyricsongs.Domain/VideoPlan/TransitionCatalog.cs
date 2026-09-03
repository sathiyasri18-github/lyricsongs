namespace Lyricsongs.Domain.VideoPlan;

public static class TransitionCatalog
{
    private static readonly IReadOnlyDictionary<TransitionType, string> FfmpegNames =
        new Dictionary<TransitionType, string>
        {
            [TransitionType.None] = "fade",
            [TransitionType.Fade] = "fade",
            [TransitionType.Dissolve] = "dissolve",
            [TransitionType.WipeLeft] = "wipeleft",
            [TransitionType.WipeRight] = "wiperight",
            [TransitionType.WipeUp] = "wipeup",
            [TransitionType.WipeDown] = "wipedown",
            [TransitionType.SlideLeft] = "slideleft",
            [TransitionType.SlideRight] = "slideright",
            [TransitionType.SlideUp] = "slideup",
            [TransitionType.SlideDown] = "slidedown",
            [TransitionType.FadeBlack] = "fadeblack",
            [TransitionType.FadeWhite] = "fadewhite",
            [TransitionType.CircleOpen] = "circleopen",
            [TransitionType.CircleClose] = "circleclose",
            [TransitionType.Radial] = "radial",
            [TransitionType.SmoothLeft] = "smoothleft",
            [TransitionType.SmoothRight] = "smoothright",
            [TransitionType.ZoomIn] = "zoomin",
            [TransitionType.Distance] = "distance",
            [TransitionType.Pixelize] = "pixelize",
            [TransitionType.DiagTopLeft] = "diagtl",
            [TransitionType.DiagTopRight] = "diagtr",
            [TransitionType.HorizontalOpen] = "horzopen",
            [TransitionType.VerticalOpen] = "vertopen"
        };

    public static string ToFfmpegName(TransitionType type) =>
        FfmpegNames.TryGetValue(type, out var name) ? name : "fade";

    public static bool TryParse(string? value, out TransitionType type)
    {
        type = TransitionType.Fade;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        if (string.Equals(normalized, "none", StringComparison.OrdinalIgnoreCase))
        {
            type = TransitionType.None;
            return true;
        }

        if (Enum.TryParse<TransitionType>(normalized, ignoreCase: true, out type))
        {
            return true;
        }

        // camelCase from API/UI (e.g. wipeLeft)
        foreach (var candidate in Enum.GetValues<TransitionType>())
        {
            if (string.Equals(ToCamel(candidate.ToString()), normalized, StringComparison.OrdinalIgnoreCase))
            {
                type = candidate;
                return true;
            }
        }

        return false;
    }

    public static string ToCamel(TransitionType type) => ToCamel(type.ToString());

    public static string ToCamel(string value) =>
        string.IsNullOrEmpty(value)
            ? value
            : char.ToLowerInvariant(value[0]) + value[1..];
}
