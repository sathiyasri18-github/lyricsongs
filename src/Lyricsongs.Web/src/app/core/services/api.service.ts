import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, catchError, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { VIDEO_TRANSITIONS, LYRIC_STYLES, findLyricStyle, findLyricPosition } from '../constants/video-catalog';
import { parseSrtContent } from '../../shared/srt/srt-parse.util';
import {
  LyricCueView,
  ProjectSummary,
  RenderJobView,
  VideoSettingsForm
} from '../models/project.models';

interface ApiRenderJobDto {
  jobId: string;
  projectId: string;
  projectName: string;
  status: string;
  progress: number;
  message?: string;
  errorMessage?: string;
  outputUrl?: string;
  createdAt: string;
  completedAt?: string;
}

interface ApiProjectDetail {
  id: string;
  name: string;
  audioFileName?: string;
  audioDurationMs?: number;
  srtFileName?: string;
  lyricCount?: number;
  assets: ApiProjectAsset[];
  settingsJson?: string;
}

interface ApiProjectAsset {
  id: string;
  fileName: string;
  type: string;
  sourceDurationMs?: number;
  sortOrder: number;
}

interface ApiSrtUploadResponse {
  fileName: string;
  isValid: boolean;
  lyricCount: number;
  lyrics: LyricCueView[];
  issues: string[];
}

interface ApiPlanResponse {
  projectId: string;
  planId: string;
  audioDurationMs: number;
  isValid: boolean;
  timeline: ApiPlanSegment[];
  lyrics: LyricCueView[];
  output: { width: number; height: number; fps: number; videoCodec: string; audioCodec: string };
  lyricsStyle: { style: string; font: string; fontSize: number };
  validationMessages: string[];
}

interface ApiPlanSegment {
  assetId: string;
  fileName: string;
  type: string;
  startMs: number;
  endMs: number;
  animation: string;
  transitionOut: string;
  transitionOutDurationMs: number;
}

export interface VideoPlanSettingsPayload {
  planningMode: string;
  randomSeed: number;
  transition: { type: string; durationMs: number };
  lyricStyle: string;
  resolutionPreset: string;
  fps: number;
  fontOverride?: string;
  fontSizeOverride?: number;
  positionOverride?: string;
  textColorOverride?: string;
  textColor2Override?: string;
  textColor3Override?: string;
  highlightColorOverride?: string;
  outlineOverride?: boolean;
  shadowOverride?: boolean;
  fadeInMsOverride?: number;
  fadeOutMsOverride?: number;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getProjects(): Observable<ProjectSummary[]> {
    return this.http.get<ProjectSummary[]>(`${this.base}/api/v1/projects`).pipe(
      catchError(() => of([]))
    );
  }

  createProject(name: string): Observable<ProjectSummary> {
    return this.http.post<ProjectSummary>(`${this.base}/api/v1/projects`, { name });
  }

  getProject(projectId: string): Observable<ApiProjectDetail> {
    return this.http.get<ApiProjectDetail>(`${this.base}/api/v1/projects/${projectId}`);
  }

  deleteProject(projectId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/v1/projects/${projectId}`);
  }

  getHealth(): Observable<{ status: string; ffmpeg: string }> {
    return this.http.get<{ status: string; ffmpeg: string }>(`${this.base}/api/v1/health`);
  }

  uploadAudio(projectId: string, file: File, durationMs?: number): Observable<unknown> {
    const fd = new FormData();
    fd.append('file', file, file.name);
    if (durationMs != null) {
      fd.append('durationMs', String(durationMs));
    }
    return this.http.post(`${this.base}/api/v1/projects/${projectId}/audio`, fd);
  }

  /** Stream URL for project audio (supports range requests for seeking). */
  projectAudioUrl(projectId: string): string {
    return `${this.base}/api/v1/projects/${projectId}/audio`;
  }

  uploadSrt(projectId: string, file: File): Observable<ApiSrtUploadResponse> {
    const fd = new FormData();
    fd.append('file', file, file.name);
    return this.http.post<ApiSrtUploadResponse>(`${this.base}/api/v1/projects/${projectId}/srt`, fd);
  }

  updateLyrics(projectId: string, lyrics: LyricCueView[]): Observable<ApiSrtUploadResponse> {
    return this.http.put<ApiSrtUploadResponse>(`${this.base}/api/v1/projects/${projectId}/srt`, {
      lyrics
    });
  }

  uploadAsset(
    projectId: string,
    file: File,
    type: 'image' | 'video',
    options?: { assetId?: string; durationMs?: number; sortOrder?: number }
  ): Observable<unknown> {
    const fd = new FormData();
    fd.append('file', file, file.name);
    fd.append('type', type);
    if (options?.assetId) fd.append('assetId', options.assetId);
    if (options?.durationMs != null) fd.append('durationMs', String(options.durationMs));
    if (options?.sortOrder != null) fd.append('sortOrder', String(options.sortOrder));
    return this.http.post(`${this.base}/api/v1/projects/${projectId}/assets`, fd);
  }

  updateSettings(projectId: string, settings: VideoPlanSettingsPayload): Observable<void> {
    return this.http.put<void>(`${this.base}/api/v1/projects/${projectId}/settings`, settings);
  }

  buildPlan(projectId: string, settings?: VideoPlanSettingsPayload): Observable<ApiPlanResponse> {
    return this.http.post<ApiPlanResponse>(
      `${this.base}/api/v1/projects/${projectId}/plan`,
      settings ?? {}
    );
  }

  reorderAssets(
    projectId: string,
    assets: { id: string; sortOrder: number }[]
  ): Observable<void> {
    return this.http.put<void>(`${this.base}/api/v1/projects/${projectId}/assets/order`, {
      assets
    });
  }

  parseSrt(file: File): Observable<{ lyrics: LyricCueView[]; isValid: boolean }> {
    return this.http
      .post<{
        lyrics: LyricCueView[];
        isValid: boolean;
      }>(`${this.base}/api/v1/subtitles/parse`, this.toFormData(file))
      .pipe(
        map((r) => ({ lyrics: r.lyrics ?? [], isValid: r.isValid })),
      );
  }

  /** Client-side SRT parse fallback for offline UI demos. */
  parseSrtLocal(content: string): LyricCueView[] {
    return parseSrtContent(content);
  }

  startRender(projectId: string, settings?: VideoPlanSettingsPayload): Observable<RenderJobView> {
    return this.http
      .post<ApiRenderJobDto>(
        `${this.base}/api/projects/${projectId}/render`,
        settings ? { settings } : {}
      )
      .pipe(map((dto) => this.toRenderJobView(dto)));
  }

  getRenderHistory(projectId?: string): Observable<RenderJobView[]> {
    const q = projectId ? `?projectId=${encodeURIComponent(projectId)}` : '';
    return this.http.get<ApiRenderJobDto[]>(`${this.base}/api/v1/render${q}`).pipe(
      map((rows) => rows.map((dto) => this.toRenderJobView(dto))),
      catchError(() => of([]))
    );
  }

  getRenderJob(jobId: string): Observable<RenderJobView> {
    return this.http
      .get<ApiRenderJobDto>(`${this.base}/api/v1/render/${jobId}`)
      .pipe(map((dto) => this.toRenderJobView(dto)));
  }

  cancelRender(jobId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/api/v1/render/${jobId}/cancel`, {});
  }

  downloadUrl(jobId: string): string {
    return `${this.base}/api/v1/render/${jobId}/download`;
  }

  parseSettingsJson(json: string): VideoSettingsForm {
    try {
      const raw = JSON.parse(json) as Record<string, unknown>;
      return {
        resolutionPreset: String(raw['resolutionPreset'] ?? 'FullHd1080p'),
        aspectRatio: String(raw['aspectRatio'] ?? 'Landscape16x9'),
        fps: Number(raw['fps'] ?? 30),
        transitionType: this.mapTransitionToUi(String((raw['transition'] as { type?: string })?.type ?? 'Fade')),
        transitionDurationMs: Number((raw['transition'] as { durationMs?: number })?.durationMs ?? 500),
        planningMode: String(raw['planningMode'] ?? 'Sequential'),
        lyricStyle: findLyricStyle(String(raw['lyricStyle'] ?? 'Cinematic')).value,
        font: String(raw['fontOverride'] ?? 'Noto Sans Tamil'),
        fontSize: Number(raw['fontSizeOverride'] ?? 54),
        position: findLyricPosition(String(raw['positionOverride'] ?? 'Bottom')).value,
        textColor: String(raw['textColorOverride'] ?? '#FFFFFF'),
        gradientColor2: String(raw['textColor2Override'] ?? '#FFD700'),
        gradientColor3: String(raw['textColor3Override'] ?? '#FF6B9D'),
        highlightColor: String(raw['highlightColorOverride'] ?? '#FFD700'),
        outline: Boolean(raw['outlineOverride'] ?? true),
        shadow: Boolean(raw['shadowOverride'] ?? true),
        lyricTextTransition:
          raw['fadeInMsOverride'] != null || raw['fadeOutMsOverride'] != null ? 'custom' : 'styleDefault',
        lyricFadeInMs: Number(raw['fadeInMsOverride'] ?? 300),
        lyricFadeOutMs: Number(raw['fadeOutMsOverride'] ?? 300)
      };
    } catch {
      return {
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
    }
  }

  private mapTransitionToUi(value: string): string {
    const normalized = (value || 'fade').trim();
    if (/^none$/i.test(normalized)) return 'none';
    const match = VIDEO_TRANSITIONS.find(
      (t) =>
        t.apiValue.toLowerCase() === normalized.toLowerCase() ||
        t.value.toLowerCase() === normalized.toLowerCase()
    );
    return match?.value ?? 'fade';
  }

  toVideoPlanSettings(form: VideoSettingsForm): VideoPlanSettingsPayload {
    const transitionType = this.mapTransition(form.transitionType);
    const useStyleFade = form.lyricTextTransition === 'styleDefault';
    return {
      planningMode: form.planningMode || 'Sequential',
      randomSeed: 42,
      transition: {
        type: transitionType,
        durationMs: form.transitionDurationMs
      },
      lyricStyle: this.mapLyricStyle(form.lyricStyle),
      resolutionPreset: form.resolutionPreset || 'FullHd1080p',
      fps: form.fps || 30,
      fontOverride: form.font,
      fontSizeOverride: form.fontSize,
      positionOverride: this.mapLyricPosition(form.position),
      textColorOverride: form.textColor,
      textColor2Override: form.gradientColor2,
      textColor3Override: form.gradientColor3,
      highlightColorOverride: form.highlightColor,
      outlineOverride: form.outline,
      shadowOverride: form.shadow,
      fadeInMsOverride: useStyleFade ? undefined : form.lyricFadeInMs,
      fadeOutMsOverride: useStyleFade ? undefined : form.lyricFadeOutMs
    };
  }

  private mapTransition(value: string): string {
    const match = VIDEO_TRANSITIONS.find(
      (t) =>
        t.value.toLowerCase() === (value || '').toLowerCase() ||
        t.apiValue.toLowerCase() === (value || '').toLowerCase()
    );
    return match?.apiValue ?? 'Fade';
  }

  private mapLyricPosition(value: string): string {
    return findLyricPosition(value).apiValue;
  }

  private mapLyricStyle(value: string): string {
    const match = LYRIC_STYLES.find(
      (s) =>
        s.value.toLowerCase() === (value || '').toLowerCase() ||
        s.apiValue.toLowerCase() === (value || '').toLowerCase()
    );
    return match?.apiValue ?? 'Cinematic';
  }

  private capitalizeStyle(value: string): string {
    if (!value) return value;
    return value.charAt(0).toUpperCase() + value.slice(1);
  }

  private toRenderJobView(dto: ApiRenderJobDto): RenderJobView {
    return {
      id: dto.jobId,
      projectId: dto.projectId,
      projectName: dto.projectName,
      status: this.mapJobStatus(dto.status),
      progressPercent: dto.progress ?? 0,
      stage: dto.status,
      message: dto.message,
      outputUrl: dto.outputUrl,
      createdAt: dto.createdAt,
      completedAt: dto.completedAt,
      errorMessage: dto.errorMessage
    };
  }

  private mapJobStatus(status: string): RenderJobView['status'] {
    switch (status) {
      case 'Queued':
        return 'queued';
      case 'Preparing':
        return 'preparing';
      case 'Rendering':
        return 'rendering';
      case 'Completed':
        return 'completed';
      case 'Failed':
        return 'failed';
      case 'Cancelled':
        return 'cancelled';
      default:
        return 'queued';
    }
  }

  private toFormData(file: File): FormData {
    const fd = new FormData();
    fd.append('file', file, file.name);
    return fd;
  }
}
