import { findLyricPosition } from '../../core/constants/video-catalog';

export { LYRIC_POSITIONS, findLyricPosition } from '../../core/constants/video-catalog';
export type { LyricPositionOption } from '../../core/constants/video-catalog';

/** Vertical anchor for canvas preview (0–1 of frame height). */
export function lyricPositionAnchorY(position: string): number {
  switch (findLyricPosition(position).value) {
    case 'top':
      return 0.12;
    case 'belowTop':
      return 0.22;
    case 'center':
      return 0.5;
    case 'aboveBottom':
      return 0.78;
    default:
      return 0.82;
  }
}

export function hasGradientText(
  textColor: string,
  gradientColor2?: string | null,
  gradientColor3?: string | null
): boolean {
  return Boolean(gradientColor2?.trim() || gradientColor3?.trim());
}

export function gradientStops(
  textColor: string,
  gradientColor2?: string | null,
  gradientColor3?: string | null
): string[] {
  const start = textColor || '#FFFFFF';
  const middle = gradientColor2?.trim() || '#FFD700';
  const end = gradientColor3?.trim() || '#FF6B9D';
  return [start, middle, end];
}

export function cssGradientText(
  textColor: string,
  gradientColor2?: string | null,
  gradientColor3?: string | null
): Record<string, string> {
  if (!hasGradientText(textColor, gradientColor2, gradientColor3)) {
    return { color: textColor };
  }

  const [start, middle, end] = gradientStops(textColor, gradientColor2, gradientColor3);
  return {
    background: `linear-gradient(90deg, ${start}, ${middle}, ${end})`,
    WebkitBackgroundClip: 'text',
    backgroundClip: 'text',
    WebkitTextFillColor: 'transparent',
    color: 'transparent'
  };
}

/** Horizontal linear gradient across measured text width. */
export function createTextGradient(
  ctx: CanvasRenderingContext2D,
  text: string,
  x: number,
  y: number,
  textColor: string,
  gradientColor2?: string | null,
  gradientColor3?: string | null
): CanvasGradient | string {
  if (!hasGradientText(textColor, gradientColor2, gradientColor3)) {
    return textColor;
  }

  const width = ctx.measureText(text).width;
  const left = x - width / 2;
  const [start, middle, end] = gradientStops(textColor, gradientColor2, gradientColor3);
  const gradient = ctx.createLinearGradient(left, y, left + width, y);
  gradient.addColorStop(0, start);
  gradient.addColorStop(0.5, middle);
  gradient.addColorStop(1, end);
  return gradient;
}
