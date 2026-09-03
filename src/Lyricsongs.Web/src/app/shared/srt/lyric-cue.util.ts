import { LyricCueView } from '../../core/models/project.models';
import { formatSrtTimecode } from './srt-time.util';

export function reindexLyrics(lyrics: LyricCueView[]): LyricCueView[] {
  return lyrics.map((cue, i) => ({ ...cue, index: i + 1 }));
}

export function createEmptyCue(startMs = 0, endMs?: number): LyricCueView {
  const start = Math.max(0, startMs);
  const end = Math.max(start + 500, endMs ?? start + 500);
  return {
    index: 0,
    start: formatSrtTimecode(start),
    end: formatSrtTimecode(end),
    text: '',
    startMs: start,
    endMs: end,
    durationMs: end - start
  };
}

/** Cue with text only — start/end at 0 until marked while audio plays. */
export function createUntimedTextCue(text: string): LyricCueView {
  return {
    index: 0,
    start: formatSrtTimecode(0),
    end: formatSrtTimecode(0),
    text,
    startMs: 0,
    endMs: 0,
    durationMs: 0
  };
}

export function updateCueTimes(cue: LyricCueView, startMs: number, endMs: number): LyricCueView {
  const start = Math.max(0, startMs);
  const end = Math.max(start + 1, endMs);
  return {
    ...cue,
    start: formatSrtTimecode(start),
    end: formatSrtTimecode(end),
    startMs: start,
    endMs: end,
    durationMs: end - start
  };
}
