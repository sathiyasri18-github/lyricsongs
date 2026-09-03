export interface LyricStyleEffect {
  alpha?: number;
  scale?: number;
  offsetX?: number;
  offsetY?: number;
  rotation?: number;
  blur?: number;
  outlineWidth?: number;
  shadowBlur?: number;
  shadowOffsetY?: number;
  useKaraoke?: boolean;
  useGradient?: boolean;
  neon?: boolean;
}

export function computeLyricStyleEffect(
  style: string,
  elapsed: number,
  remaining: number,
  cueDurationMs: number,
  fadeInMs: number,
  fadeOutMs: number
): LyricStyleEffect {
  const fadeIn = Math.min(fadeInMs, cueDurationMs / 3);
  const fadeOut = Math.min(fadeOutMs, cueDurationMs / 3);
  let alpha = 1;
  let scale = 1;
  let offsetX = 0;
  let offsetY = 0;
  let rotation = 0;
  let blur = 0;
  let outlineWidth = 3;
  let shadowBlur = 8;
  let shadowOffsetY = 2;
  let useKaraoke = false;
  let useGradient = false;
  let neon = false;

  if (elapsed < fadeIn) {
    alpha = fadeIn > 0 ? elapsed / fadeIn : 1;
  } else if (remaining < fadeOut) {
    alpha = fadeOut > 0 ? remaining / fadeOut : 1;
  }

  const key = style.toLowerCase();

  switch (key) {
    case 'pop': {
      const popMs = Math.min(250, cueDurationMs / 2);
      scale = elapsed < popMs ? 0.9 + 0.1 * (elapsed / popMs) : 1;
      break;
    }
    case 'cinematic': {
      const riseProgress = Math.min(1, elapsed / Math.max(fadeIn, 1));
      offsetY = 30 * (1 - riseProgress);
      break;
    }
    case 'bold':
      outlineWidth = 6;
      shadowBlur = 12;
      shadowOffsetY = 4;
      break;
    case 'karaoke':
      useKaraoke = true;
      alpha = 1;
      break;
    case 'neon':
      neon = true;
      shadowBlur = 18;
      shadowOffsetY = 0;
      break;
    case 'glow':
      shadowBlur = 16;
      shadowOffsetY = 0;
      blur = Math.max(0, 2 - elapsed / 200);
      break;
    case 'bounce': {
      const bounceMs = Math.min(350, cueDurationMs / 2);
      scale = elapsed < bounceMs ? 0.75 + 0.35 * (elapsed / bounceMs) : 1;
      break;
    }
    case 'slideup':
      offsetY = 80 * (1 - Math.min(1, elapsed / Math.max(fadeIn, 1)));
      break;
    case 'slidedown':
      offsetY = -80 * (1 - Math.min(1, elapsed / Math.max(fadeIn, 1)));
      break;
    case 'slideleft':
      offsetX = 120 * (1 - Math.min(1, elapsed / Math.max(fadeIn, 1)));
      break;
    case 'slideright':
      offsetX = -120 * (1 - Math.min(1, elapsed / Math.max(fadeIn, 1)));
      break;
    case 'typewriter':
      alpha = Math.min(1, elapsed / Math.max(cueDurationMs * 0.35, 400));
      break;
    case 'wave':
      offsetY = Math.sin(elapsed / 180) * 8;
      break;
    case 'pulse': {
      const phase = (elapsed % 900) / 900;
      scale = 1 + Math.sin(phase * Math.PI * 2) * 0.06;
      break;
    }
    case 'shake':
      offsetX = Math.sin(elapsed / 35) * 6;
      break;
    case 'rotate':
      rotation = -8 * (1 - Math.min(1, elapsed / Math.max(fadeIn, 1)));
      break;
    case 'blurin':
      blur = Math.max(0, 8 * (1 - Math.min(1, elapsed / Math.max(fadeIn, 1))));
      break;
    case 'shadow':
      outlineWidth = 0;
      shadowBlur = 20;
      shadowOffsetY = 6;
      break;
    case 'outline':
      outlineWidth = 6;
      shadowBlur = 0;
      break;
    case 'gradient':
      useGradient = true;
      useKaraoke = true;
      break;
    case 'retro':
      scale = 0.95 + 0.05 * Math.min(1, elapsed / 200);
      break;
    case 'elegant':
      alpha *= 0.95;
      break;
    case 'minimal':
      outlineWidth = 0;
      shadowBlur = 0;
      break;
    case 'zoomin': {
      const z = Math.min(1, elapsed / Math.max(fadeIn, 1));
      scale = 1.5 - 0.5 * z;
      break;
    }
    case 'zoomout': {
      const z = Math.min(1, elapsed / Math.max(fadeIn, 1));
      scale = 0.4 + 0.6 * z;
      break;
    }
    case 'dropin': {
      const z = Math.min(1, elapsed / Math.max(fadeIn, 1));
      offsetY = -140 * (1 - z);
      scale = 1.2 - 0.2 * z;
      break;
    }
    case 'flipin':
      rotation = -70 * (1 - Math.min(1, elapsed / Math.max(fadeIn, 1)));
      break;
    case 'spin':
      rotation = -180 * (1 - Math.min(1, elapsed / Math.max(fadeIn || 400, 1)));
      break;
    case 'drift': {
      const t = cueDurationMs > 0 ? elapsed / cueDurationMs : 0;
      offsetX = -40 + 80 * t;
      break;
    }
    case 'float': {
      const t = cueDurationMs > 0 ? elapsed / cueDurationMs : 0;
      offsetY = 24 - 40 * t;
      break;
    }
    case 'elastic': {
      const e = Math.min(350, cueDurationMs / 2);
      if (elapsed < e) {
        const p = elapsed / e;
        scale = 0.5 + 0.75 * Math.sin(p * Math.PI * 0.85);
      } else {
        scale = 1;
      }
      break;
    }
    case 'slam': {
      const s = Math.min(180, cueDurationMs / 3);
      scale = elapsed < s ? 1.8 - 0.8 * (elapsed / s) : 1;
      break;
    }
    case 'whisper':
      alpha *= 0.75 + 0.25 * Math.sin((elapsed / Math.max(cueDurationMs, 1)) * Math.PI);
      outlineWidth = 0;
      shadowBlur = 4;
      break;
    case 'softglow':
      shadowBlur = 22;
      shadowOffsetY = 0;
      blur = Math.max(0, 5 - elapsed / 100);
      break;
    case 'hardcut':
    case 'instant':
      alpha = 1;
      break;
    case 'glitch':
      offsetX = Math.sin(elapsed / 20) * 10;
      alpha = elapsed % 120 < 40 ? 0.55 : 1;
      neon = true;
      break;
    case 'risefade':
      offsetY = 60 * (1 - Math.min(1, elapsed / Math.max(fadeIn, 1)));
      break;
    case 'cascade': {
      const z = Math.min(1, elapsed / Math.max(fadeIn, 1));
      offsetX = 90 * (1 - z);
      offsetY = -70 * (1 - z);
      break;
    }
    case 'softslide':
      offsetY = 36 * (1 - Math.min(1, elapsed / Math.max(fadeIn, 1)));
      break;
    case 'slowreveal':
      alpha = Math.min(1, elapsed / Math.max(cueDurationMs * 0.45, 800));
      break;
    case 'swing': {
      const mid = cueDurationMs / 2;
      if (elapsed < mid) {
        rotation = 12 - 22 * (elapsed / mid);
      } else {
        rotation = -10 + 10 * ((elapsed - mid) / Math.max(mid, 1));
      }
      break;
    }
    case 'zoompulse': {
      const q = Math.min(cueDurationMs / 4, 200);
      if (elapsed < q) {
        scale = 1.3 - 0.3 * (elapsed / q);
      } else {
        const phase = ((elapsed - q) % 800) / 800;
        scale = 1 + Math.sin(phase * Math.PI * 2) * 0.05;
      }
      break;
    }
    case 'echo':
      outlineWidth = 0;
      shadowBlur = 28;
      shadowOffsetY = 8;
      blur = 1;
      break;
    case 'spotlight':
      blur = Math.max(0, 6 * (1 - Math.min(1, elapsed / Math.max(fadeIn, 1))));
      outlineWidth = 4;
      shadowBlur = 14;
      break;
    default:
      break;
  }

  return {
    alpha,
    scale,
    offsetX,
    offsetY,
    rotation,
    blur,
    outlineWidth,
    shadowBlur,
    shadowOffsetY,
    useKaraoke,
    useGradient,
    neon
  };
}

export function resolveLyricFadeTimes(style: string): { fadeInMs: number; fadeOutMs: number } {
  switch (style.toLowerCase()) {
    case 'minimal':
      return { fadeInMs: 150, fadeOutMs: 150 };
    case 'karaoke':
      return { fadeInMs: 0, fadeOutMs: 100 };
    case 'bold':
      return { fadeInMs: 150, fadeOutMs: 150 };
    case 'cinematic':
    case 'elegant':
    case 'drift':
    case 'float':
      return { fadeInMs: 300, fadeOutMs: 300 };
    case 'blurin':
    case 'softglow':
    case 'spotlight':
      return { fadeInMs: 400, fadeOutMs: 300 };
    case 'typewriter':
    case 'slowreveal':
      return { fadeInMs: 0, fadeOutMs: 150 };
    case 'pop':
      return { fadeInMs: 200, fadeOutMs: 0 };
    case 'hardcut':
    case 'instant':
      return { fadeInMs: 0, fadeOutMs: 0 };
    case 'whisper':
    case 'risefade':
      return { fadeInMs: 450, fadeOutMs: 450 };
    case 'glitch':
    case 'slam':
      return { fadeInMs: 100, fadeOutMs: 100 };
    default:
      return { fadeInMs: 200, fadeOutMs: 200 };
  }
}

export function resolveEffectiveLyricFade(settings: {
  lyricStyle: string;
  lyricTextTransition: string;
  lyricFadeInMs: number;
  lyricFadeOutMs: number;
}): { fadeInMs: number; fadeOutMs: number } {
  if (settings.lyricTextTransition === 'styleDefault') {
    return resolveLyricFadeTimes(settings.lyricStyle);
  }
  return {
    fadeInMs: settings.lyricFadeInMs,
    fadeOutMs: settings.lyricFadeOutMs
  };
}
