import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService, VideoPlanSettingsPayload } from './api.service';
import { ProjectStateService } from './project-state.service';
import {
  LyricCueView,
  ProjectWorkspace,
  TimelineSegmentView,
  UploadedAsset,
  VideoSettingsForm
} from '../models/project.models';

@Injectable({ providedIn: 'root' })
export class ProjectSyncService {
  private readonly api = inject(ApiService);
  private readonly state = inject(ProjectStateService);

  async ensureServerProject(name: string): Promise<string> {
    const current = this.state.ensureProject();
    if (current.serverSynced && current.id) {
      return current.id;
    }

    const created = await firstValueFrom(this.api.createProject(name));
    this.state.update({
      id: created.id,
      name: created.name,
      serverSynced: true,
      status: 'draft'
    });
    return created.id;
  }

  async uploadAudio(file: File, durationMs?: number): Promise<void> {
    const projectId = await this.ensureServerProject(this.state.ensureProject().name);
    const asset = this.state.ensureProject().audio;
    await firstValueFrom(this.api.uploadAudio(projectId, file, durationMs));
    if (asset) {
      this.state.setAudio({ ...asset, uploaded: true, serverSynced: true });
    }
  }

  async uploadSrt(file: File): Promise<LyricCueView[]> {
    const projectId = await this.ensureServerProject(this.state.ensureProject().name);
    const response = await firstValueFrom(this.api.uploadSrt(projectId, file));
    const lyrics = response.lyrics ?? [];
    const p = this.state.ensureProject();
    if (p.srt) {
      this.state.setSrt({ ...p.srt, uploaded: true, serverSynced: true }, lyrics);
    }
    return lyrics;
  }

  async syncLyrics(lyrics: LyricCueView[]): Promise<void> {
    const projectId = await this.ensureServerProject(this.state.ensureProject().name);
    const p = this.state.ensureProject();

    const response = await firstValueFrom(this.api.updateLyrics(projectId, lyrics));
    const saved = response.lyrics ?? [];
    const merged = saved.length >= lyrics.length ? saved : lyrics;
    this.state.updateLyrics(merged);
    if (!p.srt) {
      this.state.setSrt(
        {
          id: 'srt',
          fileName: response.fileName ?? 'lyrics.srt',
          type: 'srt',
          sizeBytes: 0,
          sortOrder: 0,
          uploadProgress: 100,
          uploaded: true,
          serverSynced: true
        },
        merged
      );
    }
  }

  async uploadImage(file: File, asset: UploadedAsset): Promise<void> {
    const projectId = await this.ensureServerProject(this.state.ensureProject().name);
    await firstValueFrom(
      this.api.uploadAsset(projectId, file, 'image', {
        assetId: asset.id,
        sortOrder: asset.sortOrder
      })
    );
  }

  async uploadVideo(file: File, asset: UploadedAsset, durationMs?: number): Promise<void> {
    const projectId = await this.ensureServerProject(this.state.ensureProject().name);
    await firstValueFrom(
      this.api.uploadAsset(projectId, file, 'video', {
        assetId: asset.id,
        durationMs,
        sortOrder: asset.sortOrder
      })
    );
  }

  async syncSettings(settings?: VideoSettingsForm): Promise<void> {
    const p = this.state.ensureProject();
    if (!p.serverSynced) return;
    const payload = this.api.toVideoPlanSettings(settings ?? p.settings);
    await firstValueFrom(this.api.updateSettings(p.id, payload));
  }

  async refreshPlan(settings?: VideoSettingsForm): Promise<void> {
    const p = this.state.ensureProject();
    if (!p.serverSynced || !this.state.canContinue()) {
      this.state.rebuildTimeline();
      return;
    }

    const payload = settings ? this.api.toVideoPlanSettings(settings) : undefined;
    const plan = await firstValueFrom(this.api.buildPlan(p.id, payload));

    if (plan.audioDurationMs && p.audio) {
      this.state.setAudio({ ...p.audio, durationMs: plan.audioDurationMs });
    }

    const localAssets = new Map(
      [...p.images, ...p.videos].map((a) => [a.id, a] as const)
    );

    const timeline: TimelineSegmentView[] = plan.timeline.map((seg) => {
      const local = localAssets.get(seg.assetId);
      return {
        assetId: seg.assetId,
        fileName: seg.fileName,
        type: seg.type as 'image' | 'video',
        startMs: seg.startMs,
        endMs: seg.endMs,
        animation: seg.animation,
        transitionOut: seg.transitionOut,
        transitionOutDurationMs: seg.transitionOutDurationMs,
        localUrl: local?.localUrl
      };
    });

    this.state.updateTimeline(timeline);
    this.state.update({ lyrics: plan.lyrics ?? [] });
  }

  async reorderAssets(): Promise<void> {
    const p = this.state.ensureProject();
    if (!p.serverSynced) return;

    const assets = [
      ...p.images.map((a, i) => ({ id: a.id, sortOrder: i })),
      ...p.videos.map((v, i) => ({ id: v.id, sortOrder: p.images.length + i }))
    ];
    await firstValueFrom(this.api.reorderAssets(p.id, assets));
    await this.refreshPlan();
  }

  async loadProject(projectId: string): Promise<void> {
    const detail = await firstValueFrom(this.api.getProject(projectId));
    const settings = detail.settingsJson
      ? this.api.parseSettingsJson(detail.settingsJson)
      : undefined;

    const workspace: ProjectWorkspace = {
      id: detail.id,
      name: detail.name,
      images: detail.assets
        .filter((a) => a.type === 'image')
        .map((a) => this.toAsset(a, 'image')),
      videos: detail.assets
        .filter((a) => a.type === 'video')
        .map((a) => this.toAsset(a, 'video')),
      lyrics: [],
      timeline: [],
      settings: settings ?? this.state.ensureProject().settings,
      status: detail.audioFileName ? 'ready' : 'draft',
      serverSynced: true
    };

    if (detail.audioFileName) {
      workspace.audio = {
        id: 'audio',
        fileName: detail.audioFileName,
        type: 'audio',
        sizeBytes: 0,
        durationMs: detail.audioDurationMs,
        localUrl: this.api.projectAudioUrl(projectId),
        sortOrder: 0,
        uploadProgress: 100,
        uploaded: true,
        serverSynced: true
      };
    }

    if (detail.srtFileName) {
      workspace.srt = {
        id: 'srt',
        fileName: detail.srtFileName,
        type: 'srt',
        sizeBytes: 0,
        sortOrder: 0,
        uploadProgress: 100,
        uploaded: true,
        serverSynced: true
      };
    }

    this.state.load(workspace);

    if (this.state.canContinue()) {
      try {
        const plan = await firstValueFrom(this.api.buildPlan(projectId));
        workspace.lyrics = plan.lyrics ?? [];
        const localAssets = new Map(
          [...workspace.images, ...workspace.videos].map((a) => [a.id, a] as const)
        );
        workspace.timeline = plan.timeline.map((seg) => ({
          assetId: seg.assetId,
          fileName: seg.fileName,
          type: seg.type as 'image' | 'video',
          startMs: seg.startMs,
          endMs: seg.endMs,
          animation: seg.animation,
          transitionOut: seg.transitionOut,
          transitionOutDurationMs: seg.transitionOutDurationMs,
          localUrl: localAssets.get(seg.assetId)?.localUrl
        }));
        this.state.load(workspace);
      } catch {
        this.state.rebuildTimeline();
      }
    }
  }

  private toAsset(
    dto: { id: string; fileName: string; sourceDurationMs?: number; sortOrder: number },
    type: 'image' | 'video'
  ): UploadedAsset {
    return {
      id: dto.id,
      fileName: dto.fileName,
      type,
      sizeBytes: 0,
      durationMs: dto.sourceDurationMs,
      sortOrder: dto.sortOrder,
      uploadProgress: 100,
      uploaded: true,
      serverSynced: true
    };
  }
}
