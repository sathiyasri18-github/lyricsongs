import { Injectable, signal, computed } from '@angular/core';
import {
  LyricCueView,
  ProjectWorkspace,
  TimelineSegmentView,
  UploadedAsset,
  VideoSettingsForm
} from '../models/project.models';

const DEFAULT_SETTINGS: VideoSettingsForm = {
  resolutionPreset: 'FullHd1080p',
  aspectRatio: 'Landscape16x9',
  fps: 30,
  transitionType: 'fade',
  transitionDurationMs: 500,
  planningMode: 'Sequential',
  lyricStyle: 'cinematic',
  font: 'Noto Sans Tamil',
  fontSize: 54,
  position: 'bottom',
  textColor: '#FFFFFF',
  gradientColor2: '#FFD700',
  gradientColor3: '#FF6B9D',
  highlightColor: '#FFD700',
  outline: true,
  shadow: true,
  lyricTextTransition: 'styleDefault',
  lyricFadeInMs: 300,
  lyricFadeOutMs: 300
};

@Injectable({ providedIn: 'root' })
export class ProjectStateService {
  private readonly _project = signal<ProjectWorkspace | null>(null);

  readonly project = this._project.asReadonly();

  readonly hasAudio = computed(() => !!this._project()?.audio?.uploaded);
  readonly hasSrt = computed(() => !!this._project()?.srt?.uploaded);
  readonly assetCount = computed(() => {
    const p = this._project();
    if (!p) return 0;
    return p.images.length + p.videos.length;
  });
  readonly canContinue = computed(
    () => this.hasAudio() && this.assetCount() > 0
  );
  readonly isServerSynced = computed(() => !!this._project()?.serverSynced);

  createProject(name = 'Untitled Project'): ProjectWorkspace {
    const project: ProjectWorkspace = {
      id: crypto.randomUUID(),
      name,
      images: [],
      videos: [],
      lyrics: [],
      timeline: [],
      settings: { ...DEFAULT_SETTINGS },
      status: 'draft',
      serverSynced: false
    };
    this._project.set(project);
    return project;
  }

  ensureProject(): ProjectWorkspace {
    return this._project() ?? this.createProject();
  }

  load(project: ProjectWorkspace): void {
    this._project.set(project);
  }

  update(partial: Partial<ProjectWorkspace>): void {
    const current = this.ensureProject();
    this._project.set({ ...current, ...partial });
  }

  setAudio(asset: UploadedAsset): void {
    this.update({ audio: asset });
  }

  setSrt(asset: UploadedAsset, lyrics: LyricCueView[]): void {
    this.update({ srt: asset, lyrics });
  }

  updateLyrics(lyrics: LyricCueView[]): void {
    this.update({ lyrics });
  }

  setImages(images: UploadedAsset[]): void {
    this.update({ images });
  }

  setVideos(videos: UploadedAsset[]): void {
    this.update({ videos });
  }

  updateSettings(settings: Partial<VideoSettingsForm>): void {
    const current = this.ensureProject();
    this.update({ settings: { ...current.settings, ...settings } });
  }

  reorderImages(images: UploadedAsset[]): void {
    this.setImages(images.map((img, i) => ({ ...img, sortOrder: i })));
  }

  reorderVideos(videos: UploadedAsset[]): void {
    this.setVideos(videos.map((v, i) => ({ ...v, sortOrder: i })));
  }

  updateTimeline(timeline: TimelineSegmentView[]): void {
    this.update({ timeline });
  }

  /** Client-side fallback timeline when API plan is unavailable. */
  rebuildTimeline(): void {
    const p = this.ensureProject();
    const durationMs = p.audio?.durationMs ?? 245_000;
    const assets = [
      ...p.images.map((a) => ({ ...a, kind: 'image' as const })),
      ...p.videos.map((a) => ({ ...a, kind: 'video' as const }))
    ].sort((a, b) => a.sortOrder - b.sortOrder);

    if (assets.length === 0) {
      this.update({ timeline: [] });
      return;
    }

    const slot = Math.floor(durationMs / assets.length);
    const animations = ['ZoomIn', 'ZoomOut', 'PanLeft', 'PanRight', 'SlowZoomIn'];
    let cursor = 0;
    let imageAnim = 0;

    const timeline: TimelineSegmentView[] = assets.map((asset, index) => {
      const isLast = index === assets.length - 1;
      const startMs = cursor;
      const endMs = isLast ? durationMs : cursor + slot;
      cursor = endMs;

      const animation =
        asset.kind === 'video'
          ? 'None'
          : animations[imageAnim++ % animations.length];

      return {
        assetId: asset.id,
        fileName: asset.fileName,
        type: asset.kind,
        startMs,
        endMs,
        animation,
        transitionOut: p.settings.transitionType,
        transitionOutDurationMs:
          isLast || p.settings.transitionType === 'none'
            ? 0
            : p.settings.transitionDurationMs,
        localUrl: asset.localUrl
      };
    });

    this.update({ timeline });
  }

  clear(): void {
    this._project.set(null);
  }
}
