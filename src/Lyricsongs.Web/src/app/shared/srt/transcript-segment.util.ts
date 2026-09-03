export interface TranscriptSegmentTake {
  segment: string;
  nextCursor: number;
}

export interface TranscriptSegmentCut {
  segment: string;
  remaining: string;
}

/**
 * Cuts the next pipe-delimited chunk from the start of a raw transcript.
 * Example: "Hello vanakkam | Copy the text |" → "Hello vanakkam |", remaining "Copy the text |".
 */
export function cutNextTranscriptSegment(transcript: string): TranscriptSegmentCut | null {
  const taken = takeNextTranscriptSegment(transcript, 0);
  if (!taken) {
    return null;
  }

  const remaining = transcript.slice(taken.nextCursor).replace(/^\s+/, '');
  return { segment: taken.segment, remaining };
}

/**
 * Takes the next pipe-delimited chunk from a raw transcript at cursor.
 */
export function takeNextTranscriptSegment(
  transcript: string,
  cursor: number
): TranscriptSegmentTake | null {
  if (!transcript || cursor >= transcript.length) {
    return null;
  }

  let start = cursor;
  while (start < transcript.length && /\s/.test(transcript[start]!) && transcript[start] !== '\n') {
    start++;
  }

  const pipeIndex = transcript.indexOf('|', start);
  if (pipeIndex === -1) {
    const remainder = transcript.slice(start).trim();
    if (!remainder) {
      return null;
    }
    return { segment: remainder, nextCursor: transcript.length };
  }

  const raw = transcript.slice(start, pipeIndex + 1);
  const segment = raw.replace(/^\s+/, '');
  if (!segment.trim()) {
    return takeNextTranscriptSegment(transcript, pipeIndex + 1);
  }

  return { segment, nextCursor: pipeIndex + 1 };
}

/** Split a pipe-separated transcript into lyric text segments. */
export function parseTranscriptSegments(transcript: string): string[] {
  const segments: string[] = [];
  let cursor = 0;

  while (cursor < transcript.length) {
    const taken = takeNextTranscriptSegment(transcript, cursor);
    if (!taken) {
      break;
    }
    const text = normalizeSegmentText(taken.segment);
    if (text) {
      segments.push(text);
    }
    cursor = taken.nextCursor;
  }

  return segments;
}

export function normalizeSegmentText(segment: string): string {
  return segment.replace(/\s*\|\s*$/, '').trim();
}
