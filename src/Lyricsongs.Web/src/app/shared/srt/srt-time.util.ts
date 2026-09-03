/** Parse SRT timecode (HH:MM:SS,mmm or HH:MM:SS.mmm) to milliseconds. */
export function parseSrtTimecode(value: string): number | null {
  const trimmed = value.trim();
  const match = trimmed.match(/^(\d{1,2}):(\d{2}):(\d{2})[,.](\d{1,3})$/);
  if (!match) return null;

  const hours = Number(match[1]);
  const minutes = Number(match[2]);
  const seconds = Number(match[3]);
  const millis = Number(match[4].padEnd(3, '0').slice(0, 3));

  if (minutes > 59 || seconds > 59 || millis > 999) return null;

  return hours * 3_600_000 + minutes * 60_000 + seconds * 1_000 + millis;
}

/** Format milliseconds as HH:MM:SS.mmm (API/display form). */
export function formatSrtTimecode(ms: number): string {
  const clamped = Math.max(0, Math.round(ms));
  const h = Math.floor(clamped / 3_600_000);
  const m = Math.floor((clamped % 3_600_000) / 60_000);
  const s = Math.floor((clamped % 60_000) / 1000);
  const milli = clamped % 1000;
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}.${String(milli).padStart(3, '0')}`;
}

export function isValidSrtTimecode(value: string): boolean {
  return parseSrtTimecode(value) !== null;
}
