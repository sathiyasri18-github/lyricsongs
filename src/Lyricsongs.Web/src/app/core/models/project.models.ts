export interface ProjectSummary {
  id: string;
  name: string;
  createdAt: string;
  updatedAt: string;
  status: ProjectStatus;
  audioFileName?: string;
  durationMs?: number;
  lyricCount?: number;
  assetCount?: number;
}

export type ProjectStatus =
  | 'draft'
  | 'ready'
  | 'rendering'
  | 'completed'
  | 'failed';

export interface UploadedAsset {
  id: string;
  fileName: string;
  type: 'audio' | 'srt' | 'image' | 'video';
  sizeBytes: number;
  localUrl?: string;
  /** Kept in memory so the render page can upload to the API. */
  file?: File;
  durationMs?: number;
  sortOrder: number;
  uploadProgress: number;
  uploaded: boolean;
  serverSynced?: boolean;
}

export interface LyricCueView {
  index: number;
  start: string;
  end: string;
  text: string;
  startMs: number;
  endMs: number;
  durationMs: number;
}

export interface TimelineSegmentView {
  assetId: string;
  fileName: string;
  type: 'image' | 'video';
  startMs: number;
  endMs: number;
  animation: string;
  transitionOut: string;
  transitionOutDurationMs: number;
  localUrl?: string;
}

export interface LyricStyleOption {
  id: string;
  label: string;
  description: string;
}

export interface VideoSettingsForm {
  resolutionPreset: string;
  aspectRatio: string;
  fps: number;
  transitionType: string;
  transitionDurationMs: number;
  planningMode: string;
  lyricStyle: string;
  font: string;
  fontSize: number;
  position: string;
  textColor: string;
  gradientColor2: string;
  gradientColor3: string;
  highlightColor: string;
  outline: boolean;
  shadow: boolean;
  lyricTextTransition: string;
  lyricFadeInMs: number;
  lyricFadeOutMs: number;
}

export interface RenderJobView {
  id: string;
  projectId: string;
  projectName: string;
  status: 'queued' | 'preparing' | 'rendering' | 'completed' | 'failed' | 'cancelled';
  progressPercent: number;
  stage: string;
  message?: string;
  outputUrl?: string;
  createdAt: string;
  completedAt?: string;
  errorMessage?: string;
}

export interface ProjectWorkspace {
  id: string;
  name: string;
  audio?: UploadedAsset;
  srt?: UploadedAsset;
  images: UploadedAsset[];
  videos: UploadedAsset[];
  lyrics: LyricCueView[];
  timeline: TimelineSegmentView[];
  settings: VideoSettingsForm;
  status: ProjectStatus;
  serverSynced?: boolean;
  activeJobId?: string;
}
