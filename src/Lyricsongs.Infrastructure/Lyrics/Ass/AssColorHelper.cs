using System.Globalization;
using System.Text;
using Lyricsongs.Domain.Lyrics;

namespace Lyricsongs.Infrastructure.Lyrics.Ass;

internal static class AssColorHelper
{
    /// <summary>
    /// Converts #RRGGBB to ASS &amp;HAABBGGRR (alpha 00 = opaque when opacity = 1).
    /// </summary>
    public static string ToAssColor(string hexColor, double opacity = 1.0)
    {
        var hex = hexColor.TrimStart('#');
        if (hex.Length != 6)
        {
            throw new ArgumentException($"Invalid color '{hexColor}'. Expected #RRGGBB.", nameof(hexColor));
        }

        var r = byte.Parse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var g = byte.Parse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var b = byte.Parse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var alpha = (byte)Math.Clamp((1.0 - opacity) * 255, 0, 255);

        return $"&H{alpha:X2}{b:X2}{g:X2}{r:X2}";
    }
}
