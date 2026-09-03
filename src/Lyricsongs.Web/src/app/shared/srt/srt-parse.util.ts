import { LyricCueView } from '../../core/models/project.models';
import { reindexLyrics } from './lyric-cue.util';
import { formatSrtTimecode, parseSrtTimecode } from './srt-time.util';

/** Parse raw SRT file content into lyric cues. */
export function parseSrtContent(content: string): LyricCueView[] {
  const normalized = content.replace(/^\uFEFF/, '').replace(/\r\n/g, '\n').replace(/\r/g, '\n');
  const blocks = normalized.split(/\n\s*\n/);
  const cues: LyricCueView[] = [];

  for (const block of blocks) {
    const lines = block.trim().split('\n');
    if (lines.length < 2) continue;

    let idx = 0;
    let index = cues.length + 1;
    if (/^\d+$/.test(lines[0].trim())) {
      index = Number(lines[0].trim());
      idx = 1;
    }

    const timeLine = lines[idx] ?? '';
    const match = timeLine.match(
      /(\d{1,2}:\d{2}:\d{2}[,.]\d{1,3})\s*-->\s*(\d{1,2}:\d{2}:\d{2}[,.]\d{1,3})/
    );
    if (!match) continue;

    const startMs = parseSrtTimecode(match[1]);
    const endMs = parseSrtTimecode(match[2]);
    if (startMs === null || endMs === null) continue;

    const text = lines.slice(idx + 1).join('\n').trim();
    if (!text || endMs <= startMs) continue;

    cues.push({
      index,
      start: formatSrtTimecode(startMs),
      end: formatSrtTimecode(endMs),
      text,
      startMs,
      endMs,
      durationMs: endMs - startMs
    });
  }

  return reindexLyrics(
    [...cues].sort((a, b) => a.startMs - b.startMs || a.index - b.index)
  );
}
