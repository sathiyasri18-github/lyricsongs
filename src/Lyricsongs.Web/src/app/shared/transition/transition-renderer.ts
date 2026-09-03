export type TransitionDrawCallback = (
  ctx: CanvasRenderingContext2D,
  width: number,
  height: number,
  alpha?: number
) => void;

function clipRect(
  ctx: CanvasRenderingContext2D,
  x: number,
  y: number,
  w: number,
  h: number,
  draw: TransitionDrawCallback
): void {
  ctx.save();
  ctx.beginPath();
  ctx.rect(x, y, w, h);
  ctx.clip();
  draw(ctx, 0, 0, 1);
  ctx.restore();
}

function clipCircle(
  ctx: CanvasRenderingContext2D,
  cx: number,
  cy: number,
  r: number,
  draw: TransitionDrawCallback,
  width: number,
  height: number
): void {
  ctx.save();
  ctx.beginPath();
  ctx.arc(cx, cy, Math.max(0, r), 0, Math.PI * 2);
  ctx.clip();
  draw(ctx, width, height, 1);
  ctx.restore();
}

function normalizeTransition(type: string): string {
  return type.replace(/[^a-z0-9]/gi, '').toLowerCase();
}

/** Approximate FFmpeg xfade transitions in canvas for browser preview. */
export function renderTransitionPreview(
  ctx: CanvasRenderingContext2D,
  width: number,
  height: number,
  drawFrom: TransitionDrawCallback,
  drawTo: TransitionDrawCallback,
  progress: number,
  type: string
): void {
  const t = Math.max(0, Math.min(1, progress));
  const key = normalizeTransition(type);

  ctx.clearRect(0, 0, width, height);
  drawFrom(ctx, width, height, 1);

  if (t <= 0 || key === 'none') {
    return;
  }

  ctx.save();

  switch (key) {
    case 'fade':
    case 'dissolve':
    case 'distance':
      drawTo(ctx, width, height, t);
      break;

    case 'fadeblack':
      ctx.fillStyle = `rgba(0,0,0,${t})`;
      ctx.fillRect(0, 0, width, height);
      if (t > 0.5) {
        drawTo(ctx, width, height, (t - 0.5) * 2);
      }
      break;

    case 'fadewhite':
      ctx.fillStyle = `rgba(255,255,255,${t})`;
      ctx.fillRect(0, 0, width, height);
      if (t > 0.5) {
        drawTo(ctx, width, height, (t - 0.5) * 2);
      }
      break;

    case 'wipeleft':
      clipRect(ctx, 0, 0, width * t, height, drawTo);
      break;

    case 'wiperight':
      clipRect(ctx, width * (1 - t), 0, width * t, height, drawTo);
      break;

    case 'wipeup':
      clipRect(ctx, 0, height * (1 - t), width, height * t, drawTo);
      break;

    case 'wipedown':
      clipRect(ctx, 0, 0, width, height * t, drawTo);
      break;

    case 'slideleft':
      drawTo(ctx, width, height, 1);
      ctx.clearRect(0, 0, width, height);
      drawFrom(ctx, width, height, 1);
      ctx.save();
      ctx.translate(-width * t, 0);
      drawFrom(ctx, width, height, 1);
      ctx.restore();
      ctx.save();
      ctx.translate(width * (1 - t), 0);
      drawTo(ctx, width, height, 1);
      ctx.restore();
      break;

    case 'slideright':
      drawTo(ctx, width, height, 1);
      ctx.clearRect(0, 0, width, height);
      drawFrom(ctx, width, height, 1);
      ctx.save();
      ctx.translate(width * t, 0);
      drawFrom(ctx, width, height, 1);
      ctx.restore();
      ctx.save();
      ctx.translate(-width * (1 - t), 0);
      drawTo(ctx, width, height, 1);
      ctx.restore();
      break;

    case 'slideup':
      drawTo(ctx, width, height, 1);
      ctx.clearRect(0, 0, width, height);
      drawFrom(ctx, width, height, 1);
      ctx.save();
      ctx.translate(0, -height * t);
      drawFrom(ctx, width, height, 1);
      ctx.restore();
      ctx.save();
      ctx.translate(0, height * (1 - t));
      drawTo(ctx, width, height, 1);
      ctx.restore();
      break;

    case 'slidedown':
      drawTo(ctx, width, height, 1);
      ctx.clearRect(0, 0, width, height);
      drawFrom(ctx, width, height, 1);
      ctx.save();
      ctx.translate(0, height * t);
      drawFrom(ctx, width, height, 1);
      ctx.restore();
      ctx.save();
      ctx.translate(0, -height * (1 - t));
      drawTo(ctx, width, height, 1);
      ctx.restore();
      break;

    case 'smoothleft':
    case 'smoothright':
    case 'smoothup':
    case 'smoothdown':
      drawTo(ctx, width, height, t * t);
      break;

    case 'circleopen':
    case 'radial':
      clipCircle(ctx, width / 2, height / 2, Math.hypot(width, height) * 0.5 * t, drawTo, width, height);
      break;

    case 'circleclose':
      clipCircle(
        ctx,
        width / 2,
        height / 2,
        Math.hypot(width, height) * 0.5 * (1 - t),
        drawFrom,
        width,
        height
      );
      drawTo(ctx, width, height, 1);
      break;

    case 'zoomin':
      ctx.save();
      ctx.globalAlpha = t;
      const scale = 0.85 + t * 0.15;
      ctx.translate(width / 2, height / 2);
      ctx.scale(scale, scale);
      ctx.translate(-width / 2, -height / 2);
      drawTo(ctx, width, height, 1);
      ctx.restore();
      break;

    case 'pixelize': {
      const block = Math.max(4, Math.floor(40 * (1 - t)));
      ctx.globalAlpha = t;
      for (let y = 0; y < height; y += block) {
        for (let x = 0; x < width; x += block) {
          if (Math.random() > t) continue;
          ctx.save();
          clipRect(ctx, x, y, block, block, drawTo);
          ctx.restore();
        }
      }
      break;
    }

    case 'diagtl':
      ctx.beginPath();
      ctx.moveTo(0, 0);
      ctx.lineTo(width * t, 0);
      ctx.lineTo(0, height * t);
      ctx.closePath();
      ctx.clip();
      drawTo(ctx, width, height, 1);
      break;

    case 'diagtr':
      ctx.beginPath();
      ctx.moveTo(width, 0);
      ctx.lineTo(width, height * t);
      ctx.lineTo(width * (1 - t), 0);
      ctx.closePath();
      ctx.clip();
      drawTo(ctx, width, height, 1);
      break;

    case 'horzopen':
      clipRect(ctx, width * (0.5 - t / 2), 0, width * t, height, drawTo);
      break;

    case 'vertopen':
      clipRect(ctx, 0, height * (0.5 - t / 2), width, height * t, drawTo);
      break;

    default:
      drawTo(ctx, width, height, t);
      break;
  }

  ctx.restore();
}

export function drawGradientPanel(
  ctx: CanvasRenderingContext2D,
  width: number,
  height: number,
  from: string,
  to: string,
  alpha = 1
): void {
  ctx.save();
  ctx.globalAlpha = alpha;
  const g = ctx.createLinearGradient(0, 0, width, height);
  g.addColorStop(0, from);
  g.addColorStop(1, to);
  ctx.fillStyle = g;
  ctx.fillRect(0, 0, width, height);
  ctx.restore();
}
