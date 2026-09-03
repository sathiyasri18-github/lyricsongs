using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Lyricsongs.Application.Subtitles;
using Lyricsongs.Domain.Subtitles;

namespace Lyricsongs.Infrastructure.Subtitles;

/// <summary>
/// Robust SRT parser supporting UTF-8 (with BOM), CRLF/LF, Tamil/English Unicode,
/// multiline lyrics, and graceful handling of malformed blocks.
/// </summary>
public sealed partial class SrtParser : ISrtParser
{
    private static readonly Regex ArrowRegex = TimestampLineRegex();

    public SrtParserResult Parse(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(
            stream,
            encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false),
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024,
            leaveOpen: true);

        var content = reader.ReadToEnd();
        return Parse(content);
    }

    public SrtParserResult Parse(string srtContent)
    {
        var issues = new List<SrtValidationIssue>();

        if (string.IsNullOrWhiteSpace(srtContent))
        {
            issues.Add(new SrtValidationIssue
            {
                Code = SrtValidationCode.EmptyFile,
                Message = "SRT content is empty.",
                Severity = SrtValidationSeverity.Error,
                LineNumber = 1
            });

            return new SrtParserResult
            {
                Lyrics = [],
                ParseIssues = issues
            };
        }

        // Normalize BOM if still present after StreamReader, and normalize line endings.
        var normalized = srtContent.TrimStart('\uFEFF').Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalized.Split('\n');

        var blocks = SplitIntoBlocks(lines);
        var lyrics = new List<LyricCue>();
        var nextSequentialIndex = 1;

        foreach (var block in blocks)
        {
            if (TryParseBlock(block, issues, ref nextSequentialIndex, out var cue))
            {
                lyrics.Add(cue);
            }
        }

        return new SrtParserResult
        {
            Lyrics = lyrics,
            ParseIssues = issues
        };
    }

    private static List<SrtBlock> SplitIntoBlocks(string[] lines)
    {
        var blocks = new List<SrtBlock>();
        var current = new List<(string Text, int LineNumber)>();
        var blockStartLine = 1;

        for (var i = 0; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
            {
                if (current.Count > 0)
                {
                    blocks.Add(new SrtBlock(blockStartLine, current.ToList()));
                    current.Clear();
                }

                continue;
            }

            if (current.Count == 0)
            {
                blockStartLine = lineNumber;
            }

            current.Add((line.TrimEnd(), lineNumber));
        }

        if (current.Count > 0)
        {
            blocks.Add(new SrtBlock(blockStartLine, current));
        }

        return blocks;
    }

    private static bool TryParseBlock(
        SrtBlock block,
        List<SrtValidationIssue> issues,
        ref int nextSequentialIndex,
        out LyricCue cue)
    {
        cue = null!;
        var lines = block.Lines;

        if (lines.Count == 0)
        {
            return false;
        }

        var cursor = 0;
        int? index = null;
        int? indexLine = null;

        // Optional numeric index line
        if (int.TryParse(lines[0].Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedIndex))
        {
            if (parsedIndex < 0)
            {
                issues.Add(new SrtValidationIssue
                {
                    Code = SrtValidationCode.InvalidIndex,
                    Message = $"Subtitle index '{lines[0].Text}' is invalid. Index must be a non-negative integer.",
                    Severity = SrtValidationSeverity.Error,
                    LineNumber = lines[0].LineNumber
                });
                return false;
            }

            index = parsedIndex;
            indexLine = lines[0].LineNumber;
            cursor = 1;
            nextSequentialIndex = Math.Max(nextSequentialIndex, parsedIndex + 1);
        }
        else
        {
            issues.Add(new SrtValidationIssue
            {
                Code = SrtValidationCode.MissingIndex,
                Message = $"Subtitle block starting at line {block.StartLineNumber} has no numeric index. A sequential index will be assigned if timings are valid.",
                Severity = SrtValidationSeverity.Warning,
                LineNumber = block.StartLineNumber
            });
        }

        if (cursor >= lines.Count)
        {
            issues.Add(new SrtValidationIssue
            {
                Code = SrtValidationCode.MissingTimestamp,
                Message = $"Subtitle block starting at line {block.StartLineNumber} is missing a timestamp line.",
                Severity = SrtValidationSeverity.Error,
                CueIndex = index,
                LineNumber = block.StartLineNumber
            });
            return false;
        }

        var timestampLine = lines[cursor];
        if (!TryParseTimestampLine(timestampLine.Text, out var start, out var end, out var timestampError))
        {
            // If we didn't consume an index, the first line might actually be a bad timestamp.
            var looksLikeTimestampAttempt = timestampLine.Text.Contains("-->", StringComparison.Ordinal);
            issues.Add(new SrtValidationIssue
            {
                Code = looksLikeTimestampAttempt ? SrtValidationCode.InvalidTimecode : SrtValidationCode.MissingTimestamp,
                Message = timestampError ?? $"Invalid or missing timestamp at line {timestampLine.LineNumber}.",
                Severity = SrtValidationSeverity.Error,
                CueIndex = index,
                LineNumber = timestampLine.LineNumber
            });
            return false;
        }

        cursor++;

        if (end.TotalMilliseconds < start.TotalMilliseconds)
        {
            issues.Add(new SrtValidationIssue
            {
                Code = SrtValidationCode.InvalidTimeRange,
                Message = $"Cue end time {end.ToDisplayString()} is before start time {start.ToDisplayString()}.",
                Severity = SrtValidationSeverity.Error,
                CueIndex = index,
                LineNumber = timestampLine.LineNumber
            });
            return false;
        }

        var durationMs = end.TotalMilliseconds - start.TotalMilliseconds;
        if (durationMs == 0)
        {
            issues.Add(new SrtValidationIssue
            {
                Code = SrtValidationCode.ZeroDuration,
                Message = $"Cue at line {timestampLine.LineNumber} has zero duration ({start.ToDisplayString()} --> {end.ToDisplayString()}).",
                Severity = SrtValidationSeverity.Warning,
                CueIndex = index,
                LineNumber = timestampLine.LineNumber
            });
        }

        var textLines = lines.Skip(cursor).Select(l => l.Text).ToList();
        var text = string.Join('\n', textLines).Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            issues.Add(new SrtValidationIssue
            {
                Code = SrtValidationCode.MissingText,
                Message = $"Cue starting at line {block.StartLineNumber} has no lyric text.",
                Severity = SrtValidationSeverity.Warning,
                CueIndex = index,
                LineNumber = cursor < lines.Count ? lines[cursor].LineNumber : timestampLine.LineNumber
            });
        }

        int resolvedIndex;
        if (index.HasValue)
        {
            resolvedIndex = index.Value;
        }
        else
        {
            resolvedIndex = nextSequentialIndex;
            nextSequentialIndex++;
        }

        cue = new LyricCue
        {
            Index = resolvedIndex,
            Start = start.ToDisplayString(),
            End = end.ToDisplayString(),
            Text = text,
            StartMs = start.TotalMilliseconds,
            EndMs = end.TotalMilliseconds,
            DurationMs = durationMs,
            GapToNextMs = null,
            SourceLineNumber = indexLine ?? block.StartLineNumber
        };

        return true;
    }

    private static bool TryParseTimestampLine(
        string line,
        out Timecode start,
        out Timecode end,
        out string? error)
    {
        start = default;
        end = default;
        error = null;

        var match = ArrowRegex.Match(line.Trim());
        if (!match.Success)
        {
            error = $"Expected timestamp format 'HH:MM:SS,mmm --> HH:MM:SS,mmm' but found '{line}'.";
            return false;
        }

        var startRaw = match.Groups["start"].Value.Trim();
        var endRaw = match.Groups["end"].Value.Trim();

        if (!Timecode.TryParse(startRaw, out start))
        {
            error = $"Invalid start timecode '{startRaw}'. Expected HH:MM:SS,mmm or HH:MM:SS.mmm.";
            return false;
        }

        if (!Timecode.TryParse(endRaw, out end))
        {
            error = $"Invalid end timecode '{endRaw}'. Expected HH:MM:SS,mmm or HH:MM:SS.mmm.";
            return false;
        }

        return true;
    }

    private sealed record SrtBlock(int StartLineNumber, IReadOnlyList<(string Text, int LineNumber)> Lines);

    [GeneratedRegex(@"^(?<start>.+?)\s*-->\s*(?<end>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex TimestampLineRegex();
}
