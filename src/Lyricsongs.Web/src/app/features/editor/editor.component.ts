import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { debounceTime, Subject } from 'rxjs';
import { TabsModule } from 'primeng/tabs';
import { TableModule } from 'primeng/table';
import { SelectModule } from 'primeng/select';
import { InputNumberModule } from 'primeng/inputnumber';
import { CheckboxModule } from 'primeng/checkbox';
import { ColorPickerModule } from 'primeng/colorpicker';
import { ButtonModule } from 'primeng/button';
import { OrderListModule } from 'primeng/orderlist';
import { MessageService } from 'primeng/api';
import { ProjectStateService } from '../../core/services/project-state.service';
import { ProjectSyncService } from '../../core/services/project-sync.service';
import { ApiService } from '../../core/services/api.service';
import { BackgroundTimelineComponent } from '../../shared/background-timeline/background-timeline.component';
import { LyricVideoPreviewComponent } from '../../shared/lyric-video-preview/lyric-video-preview.component';
import { UploadedAsset } from '../../core/models/project.models';
import { TAMIL_FONTS, VIDEO_TRANSITIONS, LYRIC_STYLES, LYRIC_POSITIONS, LYRIC_TEXT_TRANSITIONS, ensureGoogleFontLoaded } from '../../core/constants/video-catalog';
import { cssGradientText } from '../../shared/lyric-style/lyric-layout';
import { TransitionPreviewComponent } from '../../shared/transition/transition-preview.component';
import { SrtPreviewTableComponent } from '../../shared/srt/srt-preview-table.component';
import { TranscriptQcComponent } from '../../shared/srt/transcript-qc.component';
import { LyricCueView } from '../../core/models/project.models';

@Component({
  selector: 'app-editor',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    TabsModule,
    TableModule,
    SelectModule,
    InputNumberModule,
    CheckboxModule,
    ColorPickerModule,
    ButtonModule,
    OrderListModule,
    BackgroundTimelineComponent,
    LyricVideoPreviewComponent,
    TransitionPreviewComponent,
    SrtPreviewTableComponent,
    TranscriptQcComponent
  ],
  templateUrl: './editor.component.html',
  styleUrl: './editor.component.scss'
})
export class EditorComponent implements OnInit, OnDestroy {
  private readonly state = inject(ProjectStateService);
  private readonly sync = inject(ProjectSyncService);
  private readonly api = inject(ApiService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly messages = inject(MessageService);
  private readonly lyricsSave$ = new Subject<LyricCueView[]>();
  private lyricsSaveSub = this.lyricsSave$
    .pipe(debounceTime(600))
    .subscribe((lyrics) => {
      void this.persistLyrics(lyrics);
    });

  readonly project = this.state.project;
  selectedSegment: number | null = null;
  activeTab = '0';
  planBusy = signal(false);
  lyricsSaving = signal(false);

  readonly lyricStyles = LYRIC_STYLES.map(({ label, value }) => ({ label, value }));

  readonly resolutions = [
    { label: '1920 × 1080 (16:9)', value: 'FullHd1080p' },
    { label: '1280 × 720 (16:9)', value: 'Hd720p' },
    { label: '1080 × 1920 (9:16)', value: 'Vertical1080' },
    { label: '720 × 1280 (9:16)', value: 'Vertical720' },
    { label: '1080 × 1080 (1:1)', value: 'Square1080' }
  ];

  readonly aspectRatios = [
    { label: '16:9 Landscape', value: 'Landscape16x9' },
    { label: '9:16 Portrait', value: 'Portrait9x16' },
    { label: '1:1 Square', value: 'Square1x1' }
  ];

  readonly fpsOptions = [
    { label: '24', value: 24 },
    { label: '25', value: 25 },
    { label: '30', value: 30 },
    { label: '60', value: 60 }
  ];

  readonly transitions = VIDEO_TRANSITIONS.map(({ label, value }) => ({ label, value }));

  readonly fonts = TAMIL_FONTS.map(({ label, value }) => ({ label, value }));

  readonly positions = LYRIC_POSITIONS.map(({ label, value }) => ({ label, value }));

  readonly gradientPreviewStyle = () => {
    const v = this.settingsForm.getRawValue();
    return cssGradientText(v.textColor, v.gradientColor2, v.gradientColor3);
  };

  readonly lyricTextTransitions = LYRIC_TEXT_TRANSITIONS.map(({ label, value }) => ({ label, value }));

  readonly planningModes = [
    { label: 'Sequential', value: 'Sequential' },
    { label: 'Balanced', value: 'Balanced' },
    { label: 'Random', value: 'Random' }
  ];

  settingsForm = this.fb.nonNullable.group({
    resolutionPreset: ['FullHd1080p', Validators.required],
    aspectRatio: ['Landscape16x9', Validators.required],
    fps: [30, Validators.required],
    transitionType: ['fade', Validators.required],
    transitionDurationMs: [500, Validators.required],
    planningMode: ['Sequential', Validators.required],
    lyricStyle: ['cinematic', Validators.required],
    font: ['Noto Sans Tamil', Validators.required],
    fontSize: [54, [Validators.required, Validators.min(12), Validators.max(200)]],
    position: ['bottom', Validators.required],
    textColor: ['#FFFFFF', Validators.required],
    gradientColor2: ['#FFD700', Validators.required],
    gradientColor3: ['#FF6B9D', Validators.required],
    highlightColor: ['#FFD700', Validators.required],
    outline: [true],
    shadow: [true],
    lyricTextTransition: ['styleDefault', Validators.required],
    lyricFadeInMs: [300, [Validators.required, Validators.min(0), Validators.max(2000)]],
    lyricFadeOutMs: [300, [Validators.required, Validators.min(0), Validators.max(2000)]]
  });

  readonly previewCue = computed(() => this.project()?.lyrics[0] ?? null);

  readonly audioPlaybackUrl = computed(() => {
    const p = this.project();
    if (!p) return '';
    if (p.audio?.localUrl) return p.audio.localUrl;
    if (p.serverSynced && p.audio?.uploaded) return this.api.projectAudioUrl(p.id);
    return '';
  });

  images = signal<UploadedAsset[]>([]);
  videos = signal<UploadedAsset[]>([]);

  ngOnInit(): void {
    const p = this.state.ensureProject();
    if (!p.audio) {
      this.messages.add({
        severity: 'info',
        summary: 'Start a project',
        detail: 'Upload audio and backgrounds on the New Project screen. Lyrics are optional — add them in SRT Preview.'
      });
    }
    this.images.set([...p.images]);
    this.videos.set([...p.videos]);
    this.settingsForm.patchValue(p.settings);
    ensureGoogleFontLoaded(this.settingsForm.value.font || 'Noto Sans Tamil');
    this.settingsForm.valueChanges.pipe(debounceTime(400)).subscribe(() => {
      ensureGoogleFontLoaded(this.settingsForm.value.font || 'Noto Sans Tamil');
      void this.applySettings();
    });
    this.settingsForm.get('lyricTextTransition')?.valueChanges.subscribe((value) => {
      if (!value || value === 'styleDefault' || value === 'custom') return;
      const preset = LYRIC_TEXT_TRANSITIONS.find((t) => t.value === value);
      if (!preset) return;
      this.settingsForm.patchValue(
        { lyricFadeInMs: preset.fadeInMs, lyricFadeOutMs: preset.fadeOutMs },
        { emitEvent: false }
      );
    });
    this.settingsForm.get('lyricFadeInMs')?.valueChanges.subscribe(() => this.markLyricFadeCustom());
    this.settingsForm.get('lyricFadeOutMs')?.valueChanges.subscribe(() => this.markLyricFadeCustom());
  }

  private markLyricFadeCustom(): void {
    if (this.settingsForm.getRawValue().lyricTextTransition === 'custom') return;
    this.settingsForm.patchValue({ lyricTextTransition: 'custom' }, { emitEvent: false });
  }

  ngOnDestroy(): void {
    this.lyricsSaveSub.unsubscribe();
  }

  onLyricsChange(lyrics: LyricCueView[]): void {
    this.state.updateLyrics(lyrics);
    if (lyrics.every((c) => c.text.trim().length > 0 && c.endMs > c.startMs)) {
      this.lyricsSave$.next(lyrics);
    }
  }

  private async persistLyrics(lyrics: LyricCueView[]): Promise<void> {
    try {
      this.lyricsSaving.set(true);
      await this.sync.syncLyrics(lyrics);
    } catch (err) {
      this.messages.add({
        severity: 'error',
        summary: 'SRT save failed',
        detail: this.formatApiError(err)
      });
    } finally {
      this.lyricsSaving.set(false);
    }
  }

  async saveLyricsNow(): Promise<void> {
    const lyrics = this.state.ensureProject().lyrics;
    try {
      this.lyricsSaving.set(true);
      await this.sync.syncLyrics(lyrics);
      await this.sync.refreshPlan();
      this.messages.add({
        severity: 'success',
        summary: 'SRT saved',
        detail:
          lyrics.length > 0
            ? `${lyrics.length} cue(s) saved to the project.`
            : 'Empty SRT saved (no cues).'
      });
    } catch (err) {
      this.messages.add({
        severity: 'error',
        summary: 'SRT save failed',
        detail: this.formatApiError(err)
      });
    } finally {
      this.lyricsSaving.set(false);
    }
  }

  private formatApiError(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      const body = err.error;
      if (typeof body === 'string') return body;
      if (body && typeof body === 'object') {
        if (typeof body.detail === 'string') return body.detail;
        if (Array.isArray(body.errors)) return body.errors.join('; ');
      }
      return err.message;
    }
    return String(err);
  }

  async onImageReorder(): Promise<void> {
    this.state.reorderImages(this.images());
    try {
      this.planBusy.set(true);
      await this.sync.reorderAssets();
    } catch (err) {
      this.messages.add({ severity: 'error', summary: 'Reorder failed', detail: String(err) });
      this.state.rebuildTimeline();
    } finally {
      this.planBusy.set(false);
    }
  }

  async onVideoReorder(): Promise<void> {
    this.state.reorderVideos(this.videos());
    try {
      this.planBusy.set(true);
      await this.sync.reorderAssets();
    } catch (err) {
      this.messages.add({ severity: 'error', summary: 'Reorder failed', detail: String(err) });
      this.state.rebuildTimeline();
    } finally {
      this.planBusy.set(false);
    }
  }

  async applySettings(): Promise<void> {
    const raw = this.settingsForm.getRawValue();
    this.state.updateSettings(raw);
    if (!this.state.isServerSynced()) {
      this.state.rebuildTimeline();
      return;
    }
    try {
      this.planBusy.set(true);
      await this.sync.syncSettings(raw);
      await this.sync.refreshPlan(raw);
    } catch {
      this.state.rebuildTimeline();
    } finally {
      this.planBusy.set(false);
    }
  }

  goPreview(): void {
    this.state.updateSettings(this.settingsForm.getRawValue());
    void this.router.navigate(['/preview']);
  }

  goRender(): void {
    this.state.updateSettings(this.settingsForm.getRawValue());
    void this.router.navigate(['/render']);
  }
}

