import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TimelineSegmentView } from '../../core/models/project.models';

@Component({
  selector: 'app-background-timeline',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="timeline" *ngIf="segments.length; else empty">
      <div class="timeline__ruler">
        <span>0:00</span>
        <span>{{ formatMs(totalDurationMs / 2) }}</span>
        <span>{{ formatMs(totalDurationMs) }}</span>
      </div>

      <div class="timeline__track">
        @for (seg of segments; track seg.assetId + seg.startMs; let i = $index) {
          <button
            type="button"
            class="timeline__seg"
            [class.timeline__seg--image]="seg.type === 'image'"
            [class.timeline__seg--video]="seg.type === 'video'"
            [class.timeline__seg--active]="selectedIndex === i"
            [style.flex]="seg.endMs - seg.startMs"
            (click)="select.emit(i)"
            [title]="seg.fileName"
          >
            <span class="timeline__seg-name">{{ seg.fileName }}</span>
            <span class="timeline__seg-meta">
              {{ formatMs(seg.startMs) }} – {{ formatMs(seg.endMs) }}
            </span>
            <span class="timeline__seg-anim">{{ seg.animation }}</span>
          </button>
        }
      </div>
    </div>

    <ng-template #empty>
      <p class="timeline__empty">Upload backgrounds to build the timeline.</p>
    </ng-template>
  `,
  styles: [
    `
      .timeline {
        display: grid;
        gap: 0.75rem;
      }

      .timeline__ruler {
        display: flex;
        justify-content: space-between;
        font-size: 0.75rem;
        color: var(--ls-muted);
        letter-spacing: 0.04em;
      }

      .timeline__track {
        display: flex;
        min-height: 5.5rem;
        gap: 2px;
        overflow: hidden;
        border-radius: 4px;
        background: #0a0d11;
      }

      .timeline__seg {
        min-width: 4rem;
        border: 0;
        padding: 0.75rem;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        align-items: flex-start;
        cursor: pointer;
        text-align: left;
        color: #fff;
        transition: filter 0.15s ease, outline 0.15s ease;
      }

      .timeline__seg--image {
        background: linear-gradient(160deg, #2a3340, #1a222c);
      }

      .timeline__seg--video {
        background: linear-gradient(160deg, #3a2f1f, #1f1810);
      }

      .timeline__seg--active {
        outline: 2px solid var(--ls-accent);
        outline-offset: -2px;
        filter: brightness(1.12);
      }

      .timeline__seg-name {
        font-size: 0.78rem;
        font-weight: 600;
        max-width: 100%;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .timeline__seg-meta,
      .timeline__seg-anim {
        font-size: 0.68rem;
        opacity: 0.8;
      }

      .timeline__empty {
        margin: 0;
        padding: 2rem;
        text-align: center;
        color: var(--ls-muted);
        border: 1px dashed color-mix(in srgb, var(--ls-muted) 40%, transparent);
        border-radius: 4px;
      }
    `
  ]
})
export class BackgroundTimelineComponent {
  @Input({ required: true }) segments: TimelineSegmentView[] = [];
  @Input() totalDurationMs = 0;
  @Input() selectedIndex: number | null = null;
  @Output() select = new EventEmitter<number>();

  formatMs(ms: number): string {
    const totalSec = Math.floor(ms / 1000);
    const m = Math.floor(totalSec / 60);
    const s = totalSec % 60;
    return `${m}:${String(s).padStart(2, '0')}`;
  }
}
