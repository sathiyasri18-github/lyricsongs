using System.Globalization;
using System.Text.RegularExpressions;

namespace Lyricsongs.Domain.Subtitles;

/// <summary>
/// Represents an SRT timecode with millisecond precision.
/// Canonical display format uses dots: HH:MM:SS.mmm
/// </summary>
public readonly partial struct Timecode : IEquatable<Timecode>, IComparable<Timecode>
{
    private static readonly Regex TimecodeRegex = MyRegex();

    public long TotalMilliseconds { get; }

    public int Hours => (int)(TotalMilliseconds / 3_600_000);
    public int Minutes => (int)(TotalMilliseconds % 3_600_000 / 60_000);
    public int Seconds => (int)(TotalMilliseconds % 60_000 / 1_000);
    public int Milliseconds => (int)(TotalMilliseconds % 1_000);

    public Timecode(long totalMilliseconds)
    {
        if (totalMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalMilliseconds), "Timecode cannot be negative.");
        }

        TotalMilliseconds = totalMilliseconds;
    }

    public static Timecode FromComponents(int hours, int minutes, int seconds, int milliseconds)
    {
        if (hours < 0 || minutes is < 0 or > 59 || seconds is < 0 or > 59 || milliseconds is < 0 or > 999)
        {
            throw new ArgumentOutOfRangeException($"Invalid timecode components: {hours}:{minutes}:{seconds}.{milliseconds}");
        }

        var total = (hours * 3_600_000L) + (minutes * 60_000L) + (seconds * 1_000L) + milliseconds;
        return new Timecode(total);
    }

    /// <summary>
    /// Parses HH:MM:SS,mmm or HH:MM:SS.mmm.
    /// </summary>
    public static bool TryParse(string? value, out Timecode timecode)
    {
        timecode = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = TimecodeRegex.Match(value.Trim());
        if (!match.Success)
        {
            return false;
        }

        if (!int.TryParse(match.Groups["h"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var hours) ||
            !int.TryParse(match.Groups["m"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) ||
            !int.TryParse(match.Groups["s"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ||
            !int.TryParse(match.Groups["ms"].Value.PadRight(3, '0')[..3], NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds))
        {
            return false;
        }

        if (minutes > 59 || seconds > 59 || milliseconds > 999)
        {
            return false;
        }

        timecode = FromComponents(hours, minutes, seconds, milliseconds);
        return true;
    }

    /// <summary>
    /// Formats as HH:MM:SS.mmm (API/JSON canonical form).
    /// </summary>
    public string ToDisplayString() =>
        $"{Hours:D2}:{Minutes:D2}:{Seconds:D2}.{Milliseconds:D3}";

    /// <summary>
    /// Formats as HH:MM:SS,mmm (classic SRT form).
    /// </summary>
    public string ToSrtString() =>
        $"{Hours:D2}:{Minutes:D2}:{Seconds:D2},{Milliseconds:D3}";

    public override string ToString() => ToDisplayString();

    public bool Equals(Timecode other) => TotalMilliseconds == other.TotalMilliseconds;
    public override bool Equals(object? obj) => obj is Timecode other && Equals(other);
    public override int GetHashCode() => TotalMilliseconds.GetHashCode();
    public int CompareTo(Timecode other) => TotalMilliseconds.CompareTo(other.TotalMilliseconds);

    public static bool operator ==(Timecode left, Timecode right) => left.Equals(right);
    public static bool operator !=(Timecode left, Timecode right) => !left.Equals(right);
    public static bool operator <(Timecode left, Timecode right) => left.CompareTo(right) < 0;
    public static bool operator >(Timecode left, Timecode right) => left.CompareTo(right) > 0;
    public static bool operator <=(Timecode left, Timecode right) => left.CompareTo(right) <= 0;
    public static bool operator >=(Timecode left, Timecode right) => left.CompareTo(right) >= 0;
    public static Timecode operator +(Timecode left, Timecode right) => new(left.TotalMilliseconds + right.TotalMilliseconds);
    public static long operator -(Timecode left, Timecode right) => left.TotalMilliseconds - right.TotalMilliseconds;

    [GeneratedRegex(@"^(?<h>\d{1,2}):(?<m>\d{2}):(?<s>\d{2})[,.](?<ms>\d{1,3})$", RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();
}
