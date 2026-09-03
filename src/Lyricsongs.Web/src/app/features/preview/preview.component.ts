import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ProjectStateService } from '../../core/services/project-state.service';
import { BackgroundTimelineComponent } from '../../shared/background-timeline/background-timeline.component';
import { LyricVideoPreviewComponent } from '../../shared/lyric-video-preview/lyric-video-preview.component';

@Component({
  selector: 'app-preview',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ButtonModule,
    BackgroundTimelineComponent,
    LyricVideoPreviewComponent
  ],
  template: `
    <section class="preview" *ngIf="ready(); else missing">
      <header class="preview__head">
        <div>
          <p class="eyebrow">Preview</p>
          <h1>{{ project()!.name }}</h1>
          <p class="preview__lede">
            Browser preview synced to your MP3 — approximates the final FFmpeg lyric video.
          </p>
        </div>
        <div class="preview__actions">
          <a routerLink="/editor" pButton class="p-button-outlined" label="Back to editor"></a>
          <a routerLink="/render" pButton label="Render video" icon="pi pi-play"></a>
        </div>
      </header>

      <app-lyric-video-preview
        [audioUrl]="project()!.audio!.localUrl!"
        [durationMs]="project()!.audio!.durationMs || 0"
        [lyrics]="project()!.lyrics"
        [timeline]="project()!.timeline"
        [settings]="project()!.settings"
      />

      <h2>Background plan</h2>
      <app-background-timeline
        [segments]="project()!.timeline"
        [totalDurationMs]="project()!.audio?.durationMs || 0"
      />
    </section>

    <ng-template #missing>
      <section class="empty">
        <h1>Nothing to preview</h1>
        <p>Upload audio and at least one background on the New Project screen. Add lyrics in the editor if you skipped SRT upload.</p>
        <a routerLink="/new" pButton label="New project"></a>
      </section>
    </ng-template>
  `,
  styles: [
    `
      .preview__head {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        align-items: end;
        margin-bottom: 1.25rem;
        flex-wrap: wrap;
      }

      .preview__head h1 {
        margin: 0.1rem 0 0.35rem;
        font-family: var(--ls-font-display);
        font-size: 2.2rem;
        font-weight: 500;
      }

      .preview__lede {
        margin: 0;
        max-width: 34rem;
        color: var(--ls-muted);
      }

      .eyebrow {
        margin: 0;
        letter-spacing: 0.16em;
        text-transform: uppercase;
        font-size: 0.72rem;
        color: var(--ls-accent);
      }

      .preview__actions {
        display: flex;
        gap: 0.6rem;
      }

      h2 {
        margin: 1.5rem 0 0.75rem;
        font-family: var(--ls-font-display);
        font-weight: 500;
      }

      .empty {
        text-align: center;
        padding: 4rem 1rem;
      }

      .empty p {
        color: var(--ls-muted);
      }
    `
  ]
})
export class PreviewComponent {
  private readonly state = inject(ProjectStateService);
  readonly project = this.state.project;

  readonly ready = computed(() => {
    const p = this.project();
    return !!(p?.audio?.localUrl && p.timeline.length > 0);
  });
}
