import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { findTransition } from '../../core/constants/video-catalog';
import {
  drawGradientPanel,
  renderTransitionPreview
} from './transition-renderer';

@Component({
  selector: 'app-transition-preview',
  standalone: true,
  imports: [CommonModule, ButtonModule],
  template: `
    <div class="transition-preview">
      <div class="transition-preview__head">
        <strong>Transition preview</strong>
        <span>{{ label }}</span>
        <button
          pButton
          type="button"
          class="p-button-text p-button-sm"
          [label]="playing ? 'Pause' : 'Play'"
          [icon]="playing ? 'pi pi-pause' : 'pi pi-play'"
          (click)="toggle()"
        ></button>
      </div>
      <canvas #canvas class="transition-preview__canvas"></canvas>
    </div>
  `,
  styles: [
    `
      .transition-preview {
        margin-top: 1rem;
        border: 1px solid var(--ls-border, rgba(255, 255, 255, 0.08));
        border-radius: 12px;
        overflow: hidden;
        background: rgba(0, 0, 0, 0.2);
      }

      .transition-preview__head {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding: 0.65rem 0.85rem;
        border-bottom: 1px solid var(--ls-border, rgba(255, 255, 255, 0.08));
        font-size: 0.85rem;
      }

      .transition-preview__head span {
        color: var(--ls-muted, #9aa7b5);
      }

      .transition-preview__head button {
        margin-left: auto;
      }

      .transition-preview__canvas {
        display: block;
        width: 100%;
        aspect-ratio: 16 / 9;
        background: #0a0d11;
      }
    `
  ]
})
export class TransitionPreviewComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('canvas', { static: true }) canvasRef!: ElementRef<HTMLCanvasElement>;

  @Input({ required: true }) transitionType = 'fade';
  @Input() durationMs = 500;

  label = 'Fade';
  playing = true;
  private rafId = 0;
  private startTime = 0;
  private destroyed = false;

  ngAfterViewInit(): void {
    this.resize();
    this.restart();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['transitionType']) {
      this.label = findTransition(this.transitionType).label;
    }

    if ((changes['transitionType'] || changes['durationMs']) && this.canvasRef) {
      this.restart();
    }
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.stop();
  }

  toggle(): void {
    this.playing = !this.playing;
    if (this.playing) {
      this.startTime = performance.now();
      this.loop(this.startTime);
    } else {
      this.stop();
    }
  }

  private restart(): void {
    this.label = findTransition(this.transitionType).label;
    this.stop();
    this.resize();
    this.draw(0);
    if (this.transitionType !== 'none') {
      this.playing = true;
      this.startTime = performance.now();
      this.loop(this.startTime);
    }
  }

  private loop(now: number): void {
    if (this.destroyed || !this.playing) return;

    const duration = Math.max(this.durationMs, 300);
    const elapsed = (now - this.startTime) % (duration * 2);
    const half = duration;
    const t = elapsed <= half ? elapsed / half : 1 - (elapsed - half) / half;
    this.draw(t);

    this.rafId = requestAnimationFrame((next) => this.loop(next));
  }

  private stop(): void {
    if (this.rafId) {
      cancelAnimationFrame(this.rafId);
      this.rafId = 0;
    }
  }

  private resize(): void {
    const canvas = this.canvasRef.nativeElement;
    const width = canvas.clientWidth || 640;
    const height = Math.round((width * 9) / 16);
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    canvas.width = Math.round(width * dpr);
    canvas.height = Math.round(height * dpr);
    const ctx = canvas.getContext('2d');
    if (ctx) {
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    }
  }

  private draw(progress: number): void {
    const canvas = this.canvasRef.nativeElement;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const width = canvas.clientWidth || 640;
    const height = Math.round((width * 9) / 16);

    const drawA = (c: CanvasRenderingContext2D, w: number, h: number, alpha = 1) =>
      drawGradientPanel(c, w, h, '#2a3340', '#151b24', alpha);
    const drawB = (c: CanvasRenderingContext2D, w: number, h: number, alpha = 1) =>
      drawGradientPanel(c, w, h, '#3d2f52', '#1a2433', alpha);

    renderTransitionPreview(ctx, width, height, drawA, drawB, progress, this.transitionType);
  }
}
