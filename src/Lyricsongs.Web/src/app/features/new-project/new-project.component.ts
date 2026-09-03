import { Component, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageService } from 'primeng/api';
import { TagModule } from 'primeng/tag';
import { UploadZoneComponent } from '../../shared/upload-zone/upload-zone.component';
import { BackgroundTimelineComponent } from '../../shared/background-timeline/background-timeline.component';
import { ProjectStateService } from '../../core/services/project-state.service';
import { ProjectSyncService } from '../../core/services/project-sync.service';
import { ApiService } from '../../core/services/api.service';
import { UploadedAsset } from '../../core/models/project.models';

@Component({
  selector: 'app-new-project',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    ButtonModule,
    InputTextModule,
    TagModule,
    UploadZoneComponent,
    BackgroundTimelineComponent
  ],
  templateUrl: './new-project.component.html',
  styleUrl: './new-project.component.scss'
})
export class NewProjectComponent implements OnInit {
  private readonly state = inject(ProjectStateService);
  private readonly sync = inject(ProjectSyncService);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly messages = inject(MessageService);

  readonly project = this.state.project;
  readonly canContinue = this.state.canContinue;
  projectName = 'Untitled Lyric Video';
  selectedSegment: number | null = null;
  audioProgress = 0;
  srtProgress = 0;
  imageProgress = 0;
  videoProgress = 0;
  busy = false;
  apiOnline = true;

  readonly summary = computed(() => {
    const p = this.project();
    if (!p) return null;
    return {
      song: p.audio?.fileName ?? '—',
      duration: this.formatDuration(p.audio?.durationMs),
      lyrics: p.lyrics.length,
      assets: p.images.length + p.videos.length
    };
  });

  ngOnInit(): void {
    this.state.clear();
    this.state.createProject(this.projectName);
    this.api.getHealth().subscribe({
      next: (h) => {
        this.apiOnline = h.ffmpeg === 'available';
        if (!this.apiOnline) {
          this.messages.add({
            severity: 'warn',
            summary: 'FFmpeg not detected',
            detail: 'Install FFmpeg and restart the API before rendering.'
          });
        }
      },
      error: () => {
        this.apiOnline = false;
        this.messages.add({
          severity: 'warn',
          summary: 'API offline',
          detail: 'Start the ASP.NET API (dotnet run) for server-side planning and render.'
        });
      }
    });
  }

  async onAudio(files: File[]): Promise<void> {
    const file = files[0];
    if (!file) return;
    this.busy = true;
    this.audioProgress = 15;
    try {
      const durationMs = await this.readAudioDuration(file);
      const asset: UploadedAsset = {
        id: crypto.randomUUID(),
        fileName: file.name,
        type: 'audio',
        sizeBytes: file.size,
        localUrl: URL.createObjectURL(file),
        file,
        durationMs,
        sortOrder: 0,
        uploadProgress: 100,
        uploaded: true
      };
      this.state.setAudio(asset);
      await this.sync.ensureServerProject(this.projectName);
      await this.sync.uploadAudio(file, durationMs);
      this.audioProgress = 100;
      await this.tryRefreshPlan();
      this.messages.add({ severity: 'success', summary: 'Audio uploaded', detail: file.name });
    } catch (err) {
      this.handleError('Audio upload failed', err);
    } finally {
      this.busy = false;
    }
  }

  async onSrt(files: File[]): Promise<void> {
    const file = files[0];
    if (!file) return;
    this.busy = true;
    this.srtProgress = 30;
    try {
      await this.sync.ensureServerProject(this.projectName);
      const lyrics = await this.sync.uploadSrt(file);
      const asset: UploadedAsset = {
        id: crypto.randomUUID(),
        fileName: file.name,
        type: 'srt',
        sizeBytes: file.size,
        file,
        sortOrder: 0,
        uploadProgress: 100,
        uploaded: true,
        serverSynced: true
      };
      this.state.setSrt(asset, lyrics);
      this.srtProgress = 100;
      await this.tryRefreshPlan();
      this.messages.add({
        severity: 'success',
        summary: 'Lyrics parsed',
        detail: `${lyrics.length} Tamil/English lines`
      });
    } catch (err) {
      const text = await file.text();
      const lyrics = this.api.parseSrtLocal(text);
      const asset: UploadedAsset = {
        id: crypto.randomUUID(),
        fileName: file.name,
        type: 'srt',
        sizeBytes: file.size,
        file,
        sortOrder: 0,
        uploadProgress: 100,
        uploaded: true
      };
      this.state.setSrt(asset, lyrics);
      this.srtProgress = 100;
      this.handleError('SRT uploaded locally; API parse unavailable', err);
    } finally {
      this.busy = false;
    }
  }

  async onImages(files: File[]): Promise<void> {
    this.busy = true;
    this.imageProgress = 20;
    try {
      await this.sync.ensureServerProject(this.projectName);
      const current = this.state.ensureProject().images;
      const next = [...current];
      for (const file of files) {
        const asset: UploadedAsset = {
          id: crypto.randomUUID(),
          fileName: file.name,
          type: 'image',
          sizeBytes: file.size,
          localUrl: URL.createObjectURL(file),
          file,
          sortOrder: next.length,
          uploadProgress: 100,
          uploaded: true
        };
        next.push(asset);
        await this.sync.uploadImage(file, asset);
      }
      this.state.setImages(next);
      this.imageProgress = 100;
      await this.tryRefreshPlan();
    } catch (err) {
      this.handleError('Image upload failed', err);
    } finally {
      this.busy = false;
    }
  }

  async onVideos(files: File[]): Promise<void> {
    this.busy = true;
    this.videoProgress = 20;
    try {
      await this.sync.ensureServerProject(this.projectName);
      const current = this.state.ensureProject().videos;
      const next = [...current];
      for (const file of files) {
        const localUrl = URL.createObjectURL(file);
        const durationMs = await this.readVideoDuration(file);
        const asset: UploadedAsset = {
          id: crypto.randomUUID(),
          fileName: file.name,
          type: 'video',
          sizeBytes: file.size,
          localUrl,
          file,
          durationMs,
          sortOrder: next.length,
          uploadProgress: 100,
          uploaded: true
        };
        next.push(asset);
        await this.sync.uploadVideo(file, asset, durationMs);
      }
      this.state.setVideos(next);
      this.videoProgress = 100;
      await this.tryRefreshPlan();
    } catch (err) {
      this.handleError('Video upload failed', err);
    } finally {
      this.busy = false;
    }
  }

  async continue(): Promise<void> {
    if (!this.canContinue()) {
      this.messages.add({
        severity: 'warn',
        summary: 'Incomplete',
        detail: 'Upload audio and at least one background asset. Lyrics are optional.'
      });
      return;
    }
    this.state.update({ name: this.projectName, status: 'ready' });
    try {
      await this.sync.syncSettings();
      await this.sync.refreshPlan();
    } catch (err) {
      this.handleError('Could not refresh video plan', err);
    }
    void this.router.navigate(['/editor']);
  }

  private async tryRefreshPlan(): Promise<void> {
    if (this.state.canContinue()) {
      await this.sync.refreshPlan();
    } else {
      this.state.rebuildTimeline();
    }
  }

  private handleError(summary: string, err: unknown): void {
    const detail =
      err && typeof err === 'object' && 'error' in err
        ? String((err as { error?: { detail?: string } }).error?.detail ?? summary)
        : summary;
    this.messages.add({ severity: 'error', summary, detail });
  }

  private formatDuration(ms?: number): string {
    if (!ms) return '—';
    const total = Math.floor(ms / 1000);
    const m = Math.floor(total / 60);
    const s = total % 60;
    return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
  }

  private readAudioDuration(file: File): Promise<number> {
    return new Promise((resolve) => {
      const audio = new Audio();
      const url = URL.createObjectURL(file);
      audio.preload = 'metadata';
      audio.onloadedmetadata = () => {
        resolve(Math.round(audio.duration * 1000));
        URL.revokeObjectURL(url);
      };
      audio.onerror = () => {
        resolve(245_000);
        URL.revokeObjectURL(url);
      };
      audio.src = url;
    });
  }

  private readVideoDuration(file: File): Promise<number> {
    return new Promise((resolve) => {
      const video = document.createElement('video');
      const url = URL.createObjectURL(file);
      video.preload = 'metadata';
      video.onloadedmetadata = () => {
        resolve(Math.round(video.duration * 1000));
        URL.revokeObjectURL(url);
      };
      video.onerror = () => {
        resolve(15_000);
        URL.revokeObjectURL(url);
      };
      video.src = url;
    });
  }
}
