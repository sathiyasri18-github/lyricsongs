import { Component, OnDestroy, OnInit, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { ProgressBarModule } from 'primeng/progressbar';
import { MessageService } from 'primeng/api';
import { ProjectStateService } from '../../core/services/project-state.service';
import { ProjectSyncService } from '../../core/services/project-sync.service';
import { RenderHubService } from '../../core/services/render-hub.service';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-render',
  standalone: true,
  imports: [CommonModule, RouterLink, ButtonModule, ProgressBarModule],
  template: `
    <section class="render" *ngIf="project() as p; else missing">
      <header class="render__head">
        <div>
          <p class="eyebrow">Render</p>
          <h1>{{ p.name }}</h1>
        </div>
        <a routerLink="/editor" pButton class="p-button-text" label="Edit settings"></a>
      </header>

      <div class="render__panel">
        <div class="render__summary">
          <div><span>Audio</span><strong>{{ p.audio?.fileName || '—' }}</strong></div>
          <div><span>Lyrics</span><strong>{{ p.lyrics.length }} lines</strong></div>
          <div><span>Assets</span><strong>{{ p.images.length + p.videos.length }}</strong></div>
          <div><span>Output</span><strong>1920×1080 · H.264 · {{ p.settings.fps }}fps</strong></div>
        </div>

        @if (job(); as j) {
          <div class="render__progress">
            <div class="render__progress-head">
              <strong>{{ j.stage }}</strong>
              <span>{{ j.progressPercent | number: '1.0-0' }}%</span>
            </div>
            <p-progressBar [value]="j.progressPercent" [showValue]="false" />
            <p class="render__message">{{ j.message || 'Working…' }}</p>

            <div class="render__controls">
              @if (j.status === 'queued' || j.status === 'preparing' || j.status === 'rendering') {
                <button pButton type="button" class="p-button-outlined p-button-danger" label="Cancel" icon="pi pi-times" [disabled]="busy" (click)="cancel()"></button>
              }
              @if (j.status === 'completed') {
                <button pButton type="button" label="Download MP4" icon="pi pi-download" (click)="download()"></button>
                <a routerLink="/history" pButton class="p-button-outlined" label="View history"></a>
              }
              @if (j.status === 'failed' || j.status === 'cancelled') {
                <button pButton type="button" label="Retry" icon="pi pi-refresh" [disabled]="busy" (click)="start()"></button>
              }
            </div>

            @if (j.status === 'completed') {
              <div class="render__done">
                <i class="pi pi-check-circle"></i>
                <div>
                  <strong>Render complete</strong>
                  <p>Final MP4 is ready (H.264 + AAC). Tamil lyrics burned in via ASS.</p>
                </div>
              </div>
              @if (previewUrl(); as url) {
                <div class="render__video">
                  <h3>Final preview</h3>
                  <video controls [src]="url" width="100%"></video>
                </div>
              }
            }

            @if (j.errorMessage) {
              <p class="render__error">{{ j.errorMessage }}</p>
            }
          </div>
        } @else {
          <div class="render__start">
            <p>Queue a background FFmpeg render (1920×1080, H.264 + AAC).</p>
            <button pButton type="button" label="Start rendering" icon="pi pi-play" [disabled]="busy" (click)="start()"></button>
          </div>
        }
      </div>
    </section>

    <ng-template #missing>
      <section class="empty">
        <h1>No project loaded</h1>
        <a routerLink="/new" pButton label="New project"></a>
      </section>
    </ng-template>
  `,
  styles: [
    `
      .render__head { display: flex; justify-content: space-between; align-items: end; margin-bottom: 1.25rem; gap: 1rem; }
      .render__head h1 { margin: 0.1rem 0 0; font-family: var(--ls-font-display); font-size: 2.2rem; font-weight: 500; }
      .eyebrow { margin: 0; letter-spacing: 0.16em; text-transform: uppercase; font-size: 0.72rem; color: var(--ls-accent); }
      .render__panel { max-width: 820px; display: grid; gap: 1.25rem; }
      .render__summary { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
      .render__summary > div { padding: 0.9rem 1rem; background: var(--ls-surface); }
      .render__summary span { display: block; font-size: 0.7rem; letter-spacing: 0.08em; text-transform: uppercase; color: var(--ls-muted); margin-bottom: 0.25rem; }
      .render__progress, .render__start { padding: 1.25rem; background: var(--ls-surface); border-top: 2px solid var(--ls-accent); }
      .render__progress-head { display: flex; justify-content: space-between; margin-bottom: 0.75rem; }
      .render__message { margin: 0.75rem 0; color: var(--ls-muted); }
      .render__controls { display: flex; gap: 0.6rem; flex-wrap: wrap; }
      .render__done { margin-top: 1rem; display: flex; gap: 0.75rem; align-items: start; color: #8fd19e; }
      .render__done p { margin: 0.2rem 0 0; color: var(--ls-muted); }
      .render__video { margin-top: 1rem; }
      .render__video h3 { margin: 0 0 0.5rem; font-size: 0.95rem; }
      .render__error { color: #f0a0a0; }
      .empty { text-align: center; padding: 4rem 1rem; }
    `
  ]
})
export class RenderComponent implements OnInit, OnDestroy {
  private readonly state = inject(ProjectStateService);
  private readonly sync = inject(ProjectSyncService);
  private readonly hub = inject(RenderHubService);
  private readonly api = inject(ApiService);
  private readonly messages = inject(MessageService);

  readonly project = this.state.project;
  readonly job = this.hub.currentJob;
  readonly previewUrl = computed(() => {
    const j = this.job();
    if (!j || j.status !== 'completed') return null;
    return j.outputUrl || this.api.downloadUrl(j.id);
  });
  busy = false;

  ngOnInit(): void {
    const p = this.state.ensureProject();
    void this.hub.connect(p.id);
    if (p.activeJobId) {
      void this.resumeJob(p.activeJobId);
    }
  }

  ngOnDestroy(): void {}

  async start(): Promise<void> {
    const p = this.project();
    if (!p || !this.state.canContinue()) {
      this.messages.add({ severity: 'warn', summary: 'Not ready', detail: 'Complete uploads on New Project first.' });
      return;
    }
    if (!p.serverSynced) {
      this.messages.add({ severity: 'warn', summary: 'Not synced', detail: 'Project must be uploaded to the API before rendering.' });
      return;
    }

    this.busy = true;
    try {
      const settings = this.api.toVideoPlanSettings(p.settings);
      await this.sync.syncSettings(p.settings);
      const job = await firstValueFrom(this.api.startRender(p.id, settings));
      this.state.update({ status: 'rendering', activeJobId: job.id });
      this.hub.startLocalJob(job);
      this.messages.add({ severity: 'info', summary: 'Queued', detail: `Job ${job.id} queued for FFmpeg rendering.` });
    } catch (err: unknown) {
      this.handleError('Render failed to start', err);
      this.state.update({ status: 'ready' });
    } finally {
      this.busy = false;
    }
  }

  async cancel(): Promise<void> {
    const job = this.job();
    if (!job) return;
    try {
      await firstValueFrom(this.api.cancelRender(job.id));
    } catch {
      // ignored
    }
    this.hub.cancelLocalJob();
    this.state.update({ status: 'ready', activeJobId: undefined });
    this.messages.add({ severity: 'info', summary: 'Cancelled', detail: 'Render stopped.' });
  }

  download(): void {
    const job = this.job();
    if (!job) return;
    window.open(job.outputUrl || this.api.downloadUrl(job.id), '_blank');
  }

  private async resumeJob(jobId: string): Promise<void> {
    try {
      const job = await firstValueFrom(this.api.getRenderJob(jobId));
      this.hub.startLocalJob(job);
    } catch {
      // job may have expired
    }
  }

  private handleError(summary: string, err: unknown): void {
    const detail =
      err && typeof err === 'object' && 'error' in err
        ? String((err as { error?: { detail?: string } }).error?.detail ?? summary)
        : summary;
    this.messages.add({ severity: 'error', summary, detail });
  }
}
