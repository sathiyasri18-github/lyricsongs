import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ViewChild,
  computed,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { SliderModule } from 'primeng/slider';
import { FormsModule } from '@angular/forms';
import {
  LyricCueView,
  TimelineSegmentView,
  VideoSettingsForm
} from '../../core/models/project.models';
import { ensureGoogleFontLoaded } from '../../core/constants/video-catalog';
import { computeLyricStyleEffect, resolveEffectiveLyricFade } from '../lyric-style/lyric-style-effects';
import {
  createTextGradient,
  findLyricPosition,
  hasGradientText,
  lyricPositionAnchorY
} from '../lyric-style/lyric-layout';
import { renderTransitionPreview } from '../transition/transition-renderer';

@Component({
  selector: 'app-lyric-video-preview',
  standalone: true,
  imports: [CommonModule, ButtonModule, SliderModule, FormsModule],
  templateUrl: './lyric-video-preview.component.html',
  styleUrl: './lyric-video-preview.component.scss'
})
export class LyricVideoPreviewComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('canvas', { static: true }) canvasRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('audio', { static: true }) audioRef!: ElementRef<HTMLAudioElement>;
  @ViewChild('videoPool', { static: true }) videoPoolRef!: ElementRef<HTMLDivElement>;

  @Input({ required: true }) audioUrl = '';
  @Input() durationMs = 0;
  @Input() lyrics: LyricCueView[] = [];
  @Input() timeline: TimelineSegmentView[] = [];
  @Input() settings!: VideoSettingsForm;

  readonly playing = signal(false);
  readonly currentMs = signal(0);
  readonly scrubValue = signal(0);

  readonly currentLyric = computed(() => this.findCurrentLyric(this.currentMs()));
  readonly nextLyric = computed(() => this.findNextLyric(this.currentMs()));
  readonly currentSegment = computed(() => this.findSegment(this.currentMs()));

  private rafId = 0;
  private imageCache = new Map<string, HTMLImageElement>();
  private videoEls = new Map<string, HTMLVideoElement>();
  private ready = false;
  private destroyed = false;
  private fadeInMs = 250;
  private fadeOutMs = 250;

  ngAfterViewInit(): void {
    this.resizeCanvas();
    void this.prepareMedia().then(() => {
      this.ready = true;
      this.drawFrame(0);
    });
    window.addEventListener('resize', this.onWindowResize);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['settings'] && this.settings) {
      this.fadeInMs = this.resolveFadeIn();
      this.fadeOutMs = this.resolveFadeOut();
      ensureGoogleFontLoaded(this.settings.font || 'Noto Sans Tamil');
    }

    if ((changes['timeline'] || changes['audioUrl'] || changes['settings']) && this.canvasRef) {
      this.resizeCanvas();
      void this.prepareMedia().then(() => {
        if (!this.destroyed) {
          this.drawFrame(this.currentMs());
        }
      });
    }
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.stopLoop();
    window.removeEventListener('resize', this.onWindowResize);
    this.audioRef?.nativeElement?.pause();
    for (const video of this.videoEls.values()) {
      video.pause();
      video.removeAttribute('src');
      video.load();
    }
  }

  play(): void {
    const audio = this.audioRef.nativeElement;
    if (!this.audioUrl) return;

    void audio.play().then(() => {
      this.playing.set(true);
      this.startLoop();
    });
  }

  pause(): void {
    this.audioRef.nativeElement.pause();
    this.playing.set(false);
    this.stopLoop();
    this.pauseVideos();
  }

  restart(): void {
    const audio = this.audioRef.nativeElement;
    audio.currentTime = 0;
    this.currentMs.set(0);
    this.scrubValue.set(0);
    this.drawFrame(0);
    void audio.play().then(() => {
      this.playing.set(true);
      this.startLoop();
    });
  }

  onScrub(value: number | undefined): void {
    if (value == null) return;
    const ms = (value / 100) * this.effectiveDurationMs;
    this.seek(ms);
  }

  seek(ms: number): void {
    const clamped = Math.max(0, Math.min(ms, this.effectiveDurationMs));
    this.audioRef.nativeElement.currentTime = clamped / 1000;
    this.currentMs.set(clamped);
    this.scrubValue.set(this.effectiveDurationMs > 0 ? (clamped / this.effectiveDurationMs) * 100 : 0);
    this.drawFrame(clamped);
  }

  formatMs(ms: number): string {
    const totalSec = Math.floor(Math.max(0, ms) / 1000);
    const m = Math.floor(totalSec / 60);
    const s = totalSec % 60;
    return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
  }

  get effectiveDurationMs(): number {
    if (this.durationMs > 0) return this.durationMs;
    const audio = this.audioRef?.nativeElement;
    if (audio?.duration && Number.isFinite(audio.duration)) {
      return Math.round(audio.duration * 1000);
    }
    return this.timeline.at(-1)?.endMs ?? 0;
  }

  get aspectRatio(): string {
    switch (this.settings?.resolutionPreset) {
      case 'Vertical1080':
      case 'Vertical720':
        return '9 / 16';
      case 'Square1080':
        return '1 / 1';
      default:
        return '16 / 9';
    }
  }

  private readonly onWindowResize = (): void => {
    this.resizeCanvas();
    this.drawFrame(this.currentMs());
  };

  private startLoop(): void {
    this.stopLoop();
    const tick = (): void => {
      if (this.destroyed || !this.playing()) return;
      const ms = Math.round(this.audioRef.nativeElement.currentTime * 1000);
      this.currentMs.set(ms);
      this.scrubValue.set(this.effectiveDurationMs > 0 ? (ms / this.effectiveDurationMs) * 100 : 0);
      this.drawFrame(ms);

      if (ms >= this.effectiveDurationMs && this.effectiveDurationMs > 0) {
        this.pause();
        return;
      }

      this.rafId = requestAnimationFrame(tick);
    };
    this.rafId = requestAnimationFrame(tick);
  }

  private stopLoop(): void {
    if (this.rafId) {
      cancelAnimationFrame(this.rafId);
      this.rafId = 0;
    }
  }

  private pauseVideos(): void {
    for (const video of this.videoEls.values()) {
      if (!video.paused) video.pause();
    }
  }

  private resizeCanvas(): void {
    const canvas = this.canvasRef.nativeElement;
    const { width, height } = this.outputSize;
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    canvas.width = Math.round(width * dpr);
    canvas.height = Math.round(height * dpr);
    const ctx = canvas.getContext('2d');
    if (ctx) {
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    }
  }

  private get outputSize(): { width: number; height: number } {
    switch (this.settings?.resolutionPreset) {
      case 'Hd720p':
        return { width: 1280, height: 720 };
      case 'Vertical1080':
        return { width: 1080, height: 1920 };
      case 'Vertical720':
        return { width: 720, height: 1280 };
      case 'Square1080':
        return { width: 1080, height: 1080 };
      default:
        return { width: 1920, height: 1080 };
    }
  }

  private async prepareMedia(): Promise<void> {
    const audio = this.audioRef.nativeElement;
    if (this.audioUrl && audio.src !== this.audioUrl) {
      audio.src = this.audioUrl;
      audio.load();
    }

    await Promise.all([
      this.preloadImages(),
      this.prepareVideos()
    ]);
  }

  private async preloadImages(): Promise<void> {
    const imageSegments = this.timeline.filter((s) => s.type === 'image' && s.localUrl);
    await Promise.all(
      imageSegments.map(
        (seg) =>
          new Promise<void>((resolve) => {
            if (this.imageCache.has(seg.assetId)) {
              resolve();
              return;
            }
            const img = new Image();
            img.crossOrigin = 'anonymous';
            img.onload = () => {
              this.imageCache.set(seg.assetId, img);
              resolve();
            };
            img.onerror = () => resolve();
            img.src = seg.localUrl!;
          })
      )
    );
  }

  private async prepareVideos(): Promise<void> {
    const pool = this.videoPoolRef.nativeElement;
    const videoSegments = this.timeline.filter((s) => s.type === 'video' && s.localUrl);

    for (const seg of videoSegments) {
      let video = this.videoEls.get(seg.assetId);
      if (!video) {
        video = document.createElement('video');
        video.muted = true;
        video.playsInline = true;
        video.preload = 'auto';
        video.crossOrigin = 'anonymous';
        video.style.display = 'none';
        pool.appendChild(video);
        this.videoEls.set(seg.assetId, video);
      }
      if (video.src !== seg.localUrl) {
        video.src = seg.localUrl!;
        video.load();
      }
    }
  }

  private drawFrame(ms: number): void {
    if (!this.canvasRef) return;
    const canvas = this.canvasRef.nativeElement;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const { width, height } = this.outputSize;
    ctx.clearRect(0, 0, width, height);
    ctx.fillStyle = '#0a0d11';
    ctx.fillRect(0, 0, width, height);

    const segment = this.findSegment(ms);
    if (segment) {
      const transition = segment.transitionOutDurationMs;
      const transitionStart = segment.endMs - transition;
      const inTransition =
        transition > 0 &&
        segment.transitionOut !== 'none' &&
        ms >= transitionStart &&
        ms < segment.endMs;

      if (inTransition) {
        const next = this.findSegment(segment.endMs);
        if (next && next.assetId !== segment.assetId) {
          const t = (ms - transitionStart) / transition;
          const drawCurrent = (c: CanvasRenderingContext2D, w: number, h: number, alpha = 1) =>
            this.drawBackground(c, segment, ms, w, h, alpha);
          const drawNext = (c: CanvasRenderingContext2D, w: number, h: number, alpha = 1) =>
            this.drawBackground(c, next, ms, w, h, alpha);
          renderTransitionPreview(ctx, width, height, drawCurrent, drawNext, t, segment.transitionOut);
          this.drawLyric(ctx, ms, width, height);
          return;
        }
      }

      this.drawBackground(ctx, segment, ms, width, height);
    }

    this.drawLyric(ctx, ms, width, height);
  }

  private drawBackground(
    ctx: CanvasRenderingContext2D,
    segment: TimelineSegmentView,
    ms: number,
    width: number,
    height: number,
    alpha = 1
  ): void {
    ctx.save();
    ctx.globalAlpha = Math.max(0, Math.min(1, alpha));

    const progress = segment.endMs > segment.startMs
      ? (ms - segment.startMs) / (segment.endMs - segment.startMs)
      : 0;
    const clamped = Math.max(0, Math.min(1, progress));

    if (segment.type === 'image') {
      const img = this.imageCache.get(segment.assetId);
      if (img) {
        this.drawKenBurns(ctx, img, width, height, segment.animation, clamped);
      } else {
        this.drawPlaceholder(ctx, width, height, segment.fileName);
      }
    } else {
      const video = this.videoEls.get(segment.assetId);
      if (video && video.readyState >= 2) {
        const localTime = ((ms - segment.startMs) / 1000) % Math.max(video.duration || 1, 0.1);
        if (Math.abs(video.currentTime - localTime) > 0.12) {
          try {
            video.currentTime = localTime;
          } catch {
            // Ignore seek errors mid-load
          }
        }
        if (this.playing() && video.paused) {
          void video.play().catch(() => undefined);
        }
        this.drawCover(ctx, video, width, height, 1, 0, 0);
      } else {
        this.drawPlaceholder(ctx, width, height, segment.fileName);
      }
    }

    ctx.restore();
  }

  private drawKenBurns(
    ctx: CanvasRenderingContext2D,
    img: CanvasImageSource,
    width: number,
    height: number,
    animation: string,
    progress: number
  ): void {
    let scale = 1.08;
    let offsetX = 0;
    let offsetY = 0;

    switch (animation) {
      case 'zoomIn':
        scale = 1.0 + progress * 0.12;
        break;
      case 'zoomOut':
        scale = 1.12 - progress * 0.12;
        break;
      case 'slowZoomIn':
        scale = 1.0 + progress * 0.06;
        break;
      case 'slowZoomOut':
        scale = 1.08 - progress * 0.06;
        break;
      case 'panLeft':
        scale = 1.15;
        offsetX = progress * 0.08;
        break;
      case 'panRight':
        scale = 1.15;
        offsetX = -progress * 0.08;
        break;
      case 'panUp':
        scale = 1.15;
        offsetY = progress * 0.08;
        break;
      case 'panDown':
        scale = 1.15;
        offsetY = -progress * 0.08;
        break;
      case 'none':
      default:
        scale = 1.05;
        break;
    }

    this.drawCover(ctx, img, width, height, scale, offsetX, offsetY);
  }

  private drawCover(
    ctx: CanvasRenderingContext2D,
    source: CanvasImageSource,
    width: number,
    height: number,
    scale: number,
    offsetX: number,
    offsetY: number
  ): void {
    const sw = 'videoWidth' in source && (source as HTMLVideoElement).videoWidth
      ? (source as HTMLVideoElement).videoWidth
      : (source as HTMLImageElement).naturalWidth || width;
    const sh = 'videoHeight' in source && (source as HTMLVideoElement).videoHeight
      ? (source as HTMLVideoElement).videoHeight
      : (source as HTMLImageElement).naturalHeight || height;

    if (!sw || !sh) return;

    const cover = Math.max(width / sw, height / sh) * scale;
    const dw = sw * cover;
    const dh = sh * cover;
    const dx = (width - dw) / 2 + offsetX * width;
    const dy = (height - dh) / 2 + offsetY * height;
    ctx.drawImage(source, dx, dy, dw, dh);
  }

  private drawPlaceholder(
    ctx: CanvasRenderingContext2D,
    width: number,
    height: number,
    label: string
  ): void {
    const gradient = ctx.createLinearGradient(0, 0, width, height);
    gradient.addColorStop(0, '#2a3340');
    gradient.addColorStop(1, '#151b24');
    ctx.fillStyle = gradient;
    ctx.fillRect(0, 0, width, height);
    ctx.fillStyle = 'rgba(255,255,255,0.55)';
    ctx.font = `${Math.round(height * 0.035)}px DM Sans, sans-serif`;
    ctx.textAlign = 'center';
    ctx.fillText(label || 'Background', width / 2, height / 2);
  }

  private drawLyric(
    ctx: CanvasRenderingContext2D,
    ms: number,
    width: number,
    height: number
  ): void {
    const cue = this.findCurrentLyric(ms);
    if (!cue) return;

    const settings = this.settings;
    const style = (settings?.lyricStyle || 'cinematic').toLowerCase();
    const fontSize = settings?.fontSize || 54;
    const font = settings?.font || 'Noto Sans Tamil';
    const color = settings?.textColor || '#FFFFFF';
    const gradient2 = settings?.gradientColor2;
    const gradient3 = settings?.gradientColor3;
    const highlight = settings?.highlightColor || '#FFD700';
    const useGradient = hasGradientText(color, gradient2, gradient3);

    const elapsed = ms - cue.startMs;
    const remaining = cue.endMs - ms;
    const fadeIn = Math.min(this.fadeInMs, cue.durationMs / 3);
    const fadeOut = Math.min(this.fadeOutMs, cue.durationMs / 3);

    const effect = computeLyricStyleEffect(
      style,
      elapsed,
      remaining,
      cue.durationMs,
      fadeIn,
      fadeOut
    );

    const alpha = effect.alpha ?? 1;
    const scale = effect.scale ?? 1;
    const offsetX = effect.offsetX ?? 0;
    const offsetY = effect.offsetY ?? 0;
    const rotation = effect.rotation ?? 0;
    const outlineWidth = effect.outlineWidth ?? (settings?.outline ? 3 : 0);
    const shadow = settings?.shadow ?? true;
    const useKaraoke = effect.useKaraoke ?? style === 'karaoke';

    const lines = cue.text.split('\n');
    const lineHeight = fontSize * 1.25 * scale;
    const blockHeight = lines.length * lineHeight;

    const pos = findLyricPosition(settings?.position || 'bottom').value;
    const anchorY = lyricPositionAnchorY(settings?.position || 'bottom');
    let baseY: number;
    if (pos === 'center') {
      baseY = height * anchorY - blockHeight / 2;
    } else if (pos === 'bottom' || pos === 'aboveBottom') {
      baseY = height * anchorY - blockHeight;
    } else {
      baseY = height * anchorY;
    }

    ctx.save();
    ctx.globalAlpha = Math.max(0, Math.min(1, alpha));
    ctx.textAlign = 'center';
    ctx.textBaseline = 'top';
    ctx.font = `600 ${Math.round(fontSize * scale)}px "${font}", "Noto Sans Tamil", sans-serif`;

    if (effect.blur && effect.blur > 0) {
      ctx.filter = `blur(${effect.blur}px)`;
    }

    if (shadow && (effect.shadowBlur ?? 0) > 0) {
      ctx.shadowColor = effect.neon ? color : 'rgba(0,0,0,0.65)';
      ctx.shadowBlur = effect.shadowBlur ?? 8;
      ctx.shadowOffsetY = effect.shadowOffsetY ?? 2;
    }

    const x = width / 2 + offsetX;
    ctx.translate(x, baseY + offsetY);
    ctx.rotate((rotation * Math.PI) / 180);
    ctx.translate(-x, -(baseY + offsetY));

    lines.forEach((line, i) => {
      const y = baseY + offsetY + i * lineHeight;

      if (outlineWidth > 0) {
        ctx.lineWidth = outlineWidth;
        ctx.strokeStyle = effect.neon ? highlight : '#000000';
        ctx.strokeText(line, x, y);
      }

      if (useKaraoke) {
        this.drawKaraokeLine(ctx, line, x, y, cue, ms, color, highlight, useGradient, gradient2, gradient3);
      } else {
        ctx.fillStyle = useGradient
          ? createTextGradient(ctx, line, x, y, color, gradient2, gradient3)
          : color;
        ctx.fillText(line, x, y);
      }
    });

    ctx.restore();
  }

  private drawKaraokeLine(
    ctx: CanvasRenderingContext2D,
    line: string,
    x: number,
    y: number,
    cue: LyricCueView,
    ms: number,
    baseColor: string,
    highlight: string,
    useGradient = false,
    gradientColor2?: string | null,
    gradientColor3?: string | null
  ): void {
    const words = line.split(/(\s+)/).filter((w) => w.length > 0);
    if (words.length === 0) return;

    const progress = Math.max(0, Math.min(1, (ms - cue.startMs) / Math.max(cue.durationMs, 1)));
    const highlightCount = Math.floor(progress * words.length);

    // Measure total width for centering
    const widths = words.map((w) => ctx.measureText(w).width);
    const total = widths.reduce((a, b) => a + b, 0);
    let cursor = x - total / 2;

    words.forEach((word, i) => {
      if (i < highlightCount) {
        ctx.fillStyle = highlight;
      } else if (useGradient) {
        ctx.fillStyle = createTextGradient(ctx, word, cursor + widths[i] / 2, y, baseColor, gradientColor2, gradientColor3);
      } else {
        ctx.fillStyle = baseColor;
      }
      ctx.fillText(word, cursor + widths[i] / 2, y);
      cursor += widths[i];
    });
  }

  private resolveFadeIn(): number {
    if (!this.settings) return 250;
    return resolveEffectiveLyricFade(this.settings).fadeInMs;
  }

  private resolveFadeOut(): number {
    if (!this.settings) return 250;
    return resolveEffectiveLyricFade(this.settings).fadeOutMs;
  }

  private findSegment(ms: number): TimelineSegmentView | undefined {
    return this.timeline.find((s) => ms >= s.startMs && ms < s.endMs)
      ?? this.timeline.find((s) => ms === s.endMs && s === this.timeline.at(-1));
  }

  private findCurrentLyric(ms: number): LyricCueView | null {
    return this.lyrics.find((l) => ms >= l.startMs && ms < l.endMs) ?? null;
  }

  private findNextLyric(ms: number): LyricCueView | null {
    return this.lyrics.find((l) => l.startMs > ms) ?? null;
  }
}
