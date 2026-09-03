export interface VideoTransitionOption {
  label: string;
  value: string;
  apiValue: string;
}

export interface TamilFontOption {
  label: string;
  value: string;
  /** Google Fonts family query segment, e.g. Noto+Sans+Tamil:wght@400;600 */
  googleQuery: string;
}

export interface LyricStyleOption {
  label: string;
  value: string;
  apiValue: string;
}

export const LYRIC_STYLES: LyricStyleOption[] = [
  { label: 'Classic', value: 'classic', apiValue: 'Classic' },
  { label: 'Karaoke', value: 'karaoke', apiValue: 'Karaoke' },
  { label: 'Pop', value: 'pop', apiValue: 'Pop' },
  { label: 'Cinematic', value: 'cinematic', apiValue: 'Cinematic' },
  { label: 'Minimal', value: 'minimal', apiValue: 'Minimal' },
  { label: 'Bold', value: 'bold', apiValue: 'Bold' },
  { label: 'Neon', value: 'neon', apiValue: 'Neon' },
  { label: 'Glow', value: 'glow', apiValue: 'Glow' },
  { label: 'Bounce', value: 'bounce', apiValue: 'Bounce' },
  { label: 'Slide Up', value: 'slideUp', apiValue: 'SlideUp' },
  { label: 'Slide Down', value: 'slideDown', apiValue: 'SlideDown' },
  { label: 'Slide Left', value: 'slideLeft', apiValue: 'SlideLeft' },
  { label: 'Slide Right', value: 'slideRight', apiValue: 'SlideRight' },
  { label: 'Typewriter', value: 'typewriter', apiValue: 'Typewriter' },
  { label: 'Wave', value: 'wave', apiValue: 'Wave' },
  { label: 'Pulse', value: 'pulse', apiValue: 'Pulse' },
  { label: 'Shake', value: 'shake', apiValue: 'Shake' },
  { label: 'Rotate', value: 'rotate', apiValue: 'Rotate' },
  { label: 'Blur In', value: 'blurIn', apiValue: 'BlurIn' },
  { label: 'Shadow', value: 'shadow', apiValue: 'Shadow' },
  { label: 'Outline', value: 'outline', apiValue: 'Outline' },
  { label: 'Gradient', value: 'gradient', apiValue: 'Gradient' },
  { label: 'Retro', value: 'retro', apiValue: 'Retro' },
  { label: 'Elegant', value: 'elegant', apiValue: 'Elegant' },
  { label: 'Zoom In', value: 'zoomIn', apiValue: 'ZoomIn' },
  { label: 'Zoom Out', value: 'zoomOut', apiValue: 'ZoomOut' },
  { label: 'Drop In', value: 'dropIn', apiValue: 'DropIn' },
  { label: 'Flip In', value: 'flipIn', apiValue: 'FlipIn' },
  { label: 'Spin', value: 'spin', apiValue: 'Spin' },
  { label: 'Drift', value: 'drift', apiValue: 'Drift' },
  { label: 'Float', value: 'float', apiValue: 'Float' },
  { label: 'Elastic', value: 'elastic', apiValue: 'Elastic' },
  { label: 'Slam', value: 'slam', apiValue: 'Slam' },
  { label: 'Whisper', value: 'whisper', apiValue: 'Whisper' },
  { label: 'Soft Glow', value: 'softGlow', apiValue: 'SoftGlow' },
  { label: 'Hard Cut', value: 'hardCut', apiValue: 'HardCut' },
  { label: 'Glitch', value: 'glitch', apiValue: 'Glitch' },
  { label: 'Rise Fade', value: 'riseFade', apiValue: 'RiseFade' },
  { label: 'Cascade', value: 'cascade', apiValue: 'Cascade' },
  { label: 'Soft Slide', value: 'softSlide', apiValue: 'SoftSlide' },
  { label: 'Instant', value: 'instant', apiValue: 'Instant' },
  { label: 'Slow Reveal', value: 'slowReveal', apiValue: 'SlowReveal' },
  { label: 'Swing', value: 'swing', apiValue: 'Swing' },
  { label: 'Zoom Pulse', value: 'zoomPulse', apiValue: 'ZoomPulse' },
  { label: 'Echo', value: 'echo', apiValue: 'Echo' },
  { label: 'Spotlight', value: 'spotlight', apiValue: 'Spotlight' }
];

export function findLyricStyle(value: string | null | undefined): LyricStyleOption {
  const key = (value || 'cinematic').toLowerCase();
  return (
    LYRIC_STYLES.find(
      (s) => s.value.toLowerCase() === key || s.apiValue.toLowerCase() === key
    ) ?? LYRIC_STYLES.find((s) => s.value === 'cinematic')!
  );
}

export interface LyricTextTransitionOption {
  label: string;
  value: string;
  fadeInMs: number;
  fadeOutMs: number;
}

/** Presets for lyric entrance/exit fade (ASS \\fad tags). */
export const LYRIC_TEXT_TRANSITIONS: LyricTextTransitionOption[] = [
  { label: 'Style default', value: 'styleDefault', fadeInMs: -1, fadeOutMs: -1 },
  { label: 'None', value: 'none', fadeInMs: 0, fadeOutMs: 0 },
  { label: 'Soft', value: 'soft', fadeInMs: 200, fadeOutMs: 200 },
  { label: 'Medium', value: 'medium', fadeInMs: 300, fadeOutMs: 300 },
  { label: 'Slow', value: 'slow', fadeInMs: 500, fadeOutMs: 500 },
  { label: 'Dramatic', value: 'dramatic', fadeInMs: 800, fadeOutMs: 800 },
  { label: 'Quick in', value: 'quickIn', fadeInMs: 100, fadeOutMs: 300 },
  { label: 'Quick out', value: 'quickOut', fadeInMs: 300, fadeOutMs: 100 },
  { label: 'Custom', value: 'custom', fadeInMs: -1, fadeOutMs: -1 }
];

export function findLyricTextTransition(value: string | null | undefined): LyricTextTransitionOption {
  const key = (value || 'styleDefault').toLowerCase();
  return (
    LYRIC_TEXT_TRANSITIONS.find((t) => t.value.toLowerCase() === key) ??
    LYRIC_TEXT_TRANSITIONS[0]!
  );
}

export interface LyricPositionOption {
  label: string;
  value: string;
  apiValue: string;
}

export const LYRIC_POSITIONS: LyricPositionOption[] = [
  { label: 'Top', value: 'top', apiValue: 'Top' },
  { label: 'Below Top', value: 'belowTop', apiValue: 'BelowTop' },
  { label: 'Center', value: 'center', apiValue: 'Center' },
  { label: 'Above Bottom', value: 'aboveBottom', apiValue: 'AboveBottom' },
  { label: 'Bottom', value: 'bottom', apiValue: 'Bottom' }
];

export function findLyricPosition(value: string | null | undefined): LyricPositionOption {
  const key = (value || 'bottom').toLowerCase();
  return (
    LYRIC_POSITIONS.find(
      (p) => p.value.toLowerCase() === key || p.apiValue.toLowerCase() === key.replace(/\s+/g, '')
    ) ?? LYRIC_POSITIONS.find((p) => p.value === 'bottom')!
  );
}

export const VIDEO_TRANSITIONS: VideoTransitionOption[] = [
  { label: 'None', value: 'none', apiValue: 'None' },
  { label: 'Fade', value: 'fade', apiValue: 'Fade' },
  { label: 'Dissolve', value: 'dissolve', apiValue: 'Dissolve' },
  { label: 'Wipe Left', value: 'wipeLeft', apiValue: 'WipeLeft' },
  { label: 'Wipe Right', value: 'wipeRight', apiValue: 'WipeRight' },
  { label: 'Wipe Up', value: 'wipeUp', apiValue: 'WipeUp' },
  { label: 'Wipe Down', value: 'wipeDown', apiValue: 'WipeDown' },
  { label: 'Slide Left', value: 'slideLeft', apiValue: 'SlideLeft' },
  { label: 'Slide Right', value: 'slideRight', apiValue: 'SlideRight' },
  { label: 'Slide Up', value: 'slideUp', apiValue: 'SlideUp' },
  { label: 'Slide Down', value: 'slideDown', apiValue: 'SlideDown' },
  { label: 'Fade Black', value: 'fadeBlack', apiValue: 'FadeBlack' },
  { label: 'Fade White', value: 'fadeWhite', apiValue: 'FadeWhite' },
  { label: 'Circle Open', value: 'circleOpen', apiValue: 'CircleOpen' },
  { label: 'Circle Close', value: 'circleClose', apiValue: 'CircleClose' },
  { label: 'Radial', value: 'radial', apiValue: 'Radial' },
  { label: 'Smooth Left', value: 'smoothLeft', apiValue: 'SmoothLeft' },
  { label: 'Smooth Right', value: 'smoothRight', apiValue: 'SmoothRight' },
  { label: 'Zoom In', value: 'zoomIn', apiValue: 'ZoomIn' },
  { label: 'Distance', value: 'distance', apiValue: 'Distance' },
  { label: 'Pixelize', value: 'pixelize', apiValue: 'Pixelize' },
  { label: 'Diagonal Top-Left', value: 'diagTopLeft', apiValue: 'DiagTopLeft' },
  { label: 'Diagonal Top-Right', value: 'diagTopRight', apiValue: 'DiagTopRight' },
  { label: 'Horizontal Open', value: 'horizontalOpen', apiValue: 'HorizontalOpen' },
  { label: 'Vertical Open', value: 'verticalOpen', apiValue: 'VerticalOpen' }
];

export const TAMIL_FONTS: TamilFontOption[] = [
  { label: 'Noto Sans Tamil', value: 'Noto Sans Tamil', googleQuery: 'Noto+Sans+Tamil:wght@400;600' },
  { label: 'Noto Serif Tamil', value: 'Noto Serif Tamil', googleQuery: 'Noto+Serif+Tamil:wght@400;600' },
  { label: 'Hind Madurai', value: 'Hind Madurai', googleQuery: 'Hind+Madurai:wght@400;600;700' },
  { label: 'Mukta Malar', value: 'Mukta Malar', googleQuery: 'Mukta+Malar:wght@400;600;700' },
  { label: 'Tiro Tamil', value: 'Tiro Tamil', googleQuery: 'Tiro+Tamil:ital@0;1' },
  { label: 'Arima Madurai', value: 'Arima Madurai', googleQuery: 'Arima+Madurai:wght@400;600;700' },
  { label: 'Meera Inimai', value: 'Meera Inimai', googleQuery: 'Meera+Inimai' },
  { label: 'Kavivanar', value: 'Kavivanar', googleQuery: 'Kavivanar' },
  { label: 'Baloo Thambi 2', value: 'Baloo Thambi 2', googleQuery: 'Baloo+Thambi+2:wght@400;600;700' },
  { label: 'Catamaran', value: 'Catamaran', googleQuery: 'Catamaran:wght@400;600;700' },
  { label: 'Anek Tamil', value: 'Anek Tamil', googleQuery: 'Anek+Tamil:wght@400;600;700' },
  { label: 'Karla Tamil Upright', value: 'Karla Tamil Upright', googleQuery: 'Karla+Tamil+Upright:wght@400;600;700' },
  { label: 'Rubik', value: 'Rubik', googleQuery: 'Rubik:wght@400;600;700' },
  { label: 'Shantell Sans', value: 'Shantell Sans', googleQuery: 'Shantell+Sans:wght@400;600' },
  { label: 'Tillana', value: 'Tillana', googleQuery: 'Tillana:wght@400;600;700' },
  { label: 'Poppins', value: 'Poppins', googleQuery: 'Poppins:wght@400;600;700' },
  { label: 'Montserrat', value: 'Montserrat', googleQuery: 'Montserrat:wght@400;600;700' },
  { label: 'DM Sans', value: 'DM Sans', googleQuery: 'DM+Sans:opsz,wght@9..40,400;9..40,600' },
  { label: 'Lato', value: 'Lato', googleQuery: 'Lato:wght@400;700' },
  { label: 'Roboto', value: 'Roboto', googleQuery: 'Roboto:wght@400;700' },
  { label: 'Open Sans', value: 'Open Sans', googleQuery: 'Open+Sans:wght@400;600;700' },
  { label: 'Nunito', value: 'Nunito', googleQuery: 'Nunito:wght@400;600;700' },
  { label: 'Source Sans 3', value: 'Source Sans 3', googleQuery: 'Source+Sans+3:wght@400;600;700' },
  { label: 'Inter', value: 'Inter', googleQuery: 'Inter:wght@400;600;700' }
];

export function findTransition(value: string | null | undefined): VideoTransitionOption {
  const key = (value || 'fade').toLowerCase();
  return (
    VIDEO_TRANSITIONS.find((t) => t.value.toLowerCase() === key || t.apiValue.toLowerCase() === key) ??
    VIDEO_TRANSITIONS.find((t) => t.value === 'fade')!
  );
}

export function buildGoogleFontsHref(): string {
  const uiFamilies = 'Fraunces:opsz,wght@9..144,500;9..144,600';
  const tamilFamilies = TAMIL_FONTS.map((f) => f.googleQuery).join('&family=');
  return `https://fonts.googleapis.com/css2?family=${uiFamilies}&family=${tamilFamilies}&display=swap`;
}

export function ensureGoogleFontLoaded(fontFamily: string): void {
  const font = TAMIL_FONTS.find((f) => f.value === fontFamily);
  if (!font) return;

  const id = `gf-${font.value.replace(/\s+/g, '-').toLowerCase()}`;
  if (document.getElementById(id)) return;

  const link = document.createElement('link');
  link.id = id;
  link.rel = 'stylesheet';
  link.href = `https://fonts.googleapis.com/css2?family=${font.googleQuery}&display=swap`;
  document.head.appendChild(link);
}
