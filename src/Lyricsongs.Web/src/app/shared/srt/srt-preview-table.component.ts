import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  OnChanges,
  OnDestroy,
  Output,
  SimpleChanges,
  ViewChild,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { ButtonModule } from 'primeng/button';
import { SliderModule } from 'primeng/slider';
import { LyricCueView } from '../../core/models/project.models';
import { createEmptyCue, reindexLyrics, updateCueTimes } from './lyric-cue.util';
import { formatSrtTimecode, parseSrtTimecode } from './srt-time.util';

type FieldKey = 'start' | 'end' | 'text';

const PLAYBACK_RATES = [0.65, 0.7, 0.75, 0.8, 0.85, 0.9, 0.95, 1] as const;

/** Compensate for clicking Mark In/Out ~1–2 s after hearing the moment. */
const MARK_REACTION_OFFSET_MS = 2_000;

@Component({
  selector: 'app-srt-preview-table',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    InputTextModule,
    TextareaModule,
    ButtonModule,
    SliderModule
  ],
  template: `
    <div class="srt-check">
      @if (audioUrl) {
        <div class="srt-check__player">
          <audio #audio preload="auto" (ended)="onEnded()"></audio>

          <div class="srt-check__controls">
            <button
              pButton
              type="button"
              class="p-button-sm"
              [icon]="playing() ? 'pi pi-pause' : 'pi pi-play'"
              [label]="playing() ? 'Pause' : 'Play'"
              (click)="togglePlay()"
            ></button>
            <button
              pButton
              type="button"
              class="p-button-sm p-button-outlined"
              icon="pi pi-replay"
              label="Restart"
              (click)="restart()"
            ></button>
            <button
              pButton
              type="button"
              class="p-button-sm p-button-outlined"
              icon="pi pi-flag"
              label="Mark In"
              title="Set start time of selected cue (I)"
              [disabled]="selectedIndex() === null"
              (click)="markIn()"
            ></button>
            <button
              pButton
              type="button"
              class="p-button-sm p-button-outlined"
              icon="pi pi-flag-fill"
              label="Mark Out"
              title="Set end time of selected cue (O)"
              [disabled]="selectedIndex() === null"
              (click)="markOut()"
            ></button>
            <button
              pButton
              type="button"
              class="p-button-sm p-button-outlined"
              icon="pi pi-plus"
              label="Add cue"
              title="Add cue at playhead (P)"
              (click)="addCue()"
            ></button>
            <button
              pButton
              type="button"
              class="p-button-sm"
              icon="pi pi-save"
              label="Save SRT"
              title="Write cues to the project SRT file"
              [loading]="saving"
              [disabled]="saving"
              (click)="saveRequest.emit()"
            ></button>
            @if (cueStopMs !== null) {
              <button
                pButton
                type="button"
                class="p-button-sm p-button-text"
                icon="pi pi-times"
                label="Stop cue loop"
                (click)="clearCueStop()"
              ></button>
            }
          </div>

          <div class="srt-check__speed">
            <span class="srt-check__speed-label">Speed</span>
            @for (rate of playbackRates; track rate) {
              <button
                pButton
                type="button"
                class="p-button-sm srt-check__speed-btn"
                [class.p-button-outlined]="playbackRate() !== rate"
                [label]="formatRate(rate)"
                [title]="'Play at ' + formatRate(rate) + ' speed'"
                (click)="setPlaybackRate(rate)"
              ></button>
            }
          </div>

          <div class="srt-check__scrub">
            <span class="srt-check__time">{{ formatMs(currentMs()) }}</span>
            <p-slider
              class="srt-check__slider"
              [ngModel]="scrubValue()"
              (ngModelChange)="onScrub($event)"
              [min]="0"
              [max]="100"
              [step]="0.05"
            />
            <span class="srt-check__time">{{ formatMs(effectiveDurationMs) }}</span>
          </div>

          <div class="srt-check__meta">
            <div class="srt-check__now">
              <span class="srt-check__now-label">Now playing</span>
              <strong class="srt-check__now-text">{{ activeCue()?.text || '—' }}</strong>
              @if (activeCue(); as cue) {
                <small>{{ cue.start }} → {{ cue.end }}</small>
              }
            </div>
            @if (selectedCue(); as sel) {
              <div class="srt-check__selected">
                <span class="srt-check__now-label">Selected cue #{{ sel.index }}</span>
                <small>{{ sel.start }} → {{ sel.end }}</small>
                <span class="srt-check__shortcut">Shortcuts: P = add cue · I = mark in · O = mark out · Space = play/pause</span>
              </div>
            } @else {
              <p class="srt-check__select-hint">Select a row to use Mark In / Mark Out.</p>
            }
          </div>
        </div>
      } @else {
        <p class="srt-check__missing">Upload project audio to play and check cue timings.</p>
      }

      <p-table
        [value]="lyrics"
        [paginator]="true"
        [rows]="10"
        styleClass="p-datatable-sm srt-table"
        [rowTrackBy]="trackByIndex"
      >
        <ng-template pTemplate="header">
          <tr>
            <th style="width:4.5rem">Play</th>
            <th style="width:3rem">#</th>
            <th style="width:12rem">Start</th>
            <th style="width:12rem">End</th>
            <th>Text</th>
            <th style="width:7rem">Actions</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-row>
          <tr
            class="srt-table__row"
            [class.srt-table__row--active]="activeCue()?.index === row.index"
            [class.srt-table__row--selected]="selectedIndex() === row.index"
            (click)="selectRow(row)"
          >
            <td (click)="$event.stopPropagation()">
              <button
                pButton
                type="button"
                class="p-button-rounded p-button-text p-button-sm"
                icon="pi pi-play"
                [disabled]="!audioUrl"
                title="Play this cue"
                (click)="playCue(row)"
                aria-label="Play cue"
              ></button>
            </td>
            <td>{{ row.index }}</td>
            <td (click)="$event.stopPropagation()">
              <div class="srt-table__time-cell">
                <input
                  pInputText
                  class="srt-table__time"
                  [class.srt-table__time--invalid]="invalidField(row.index, 'start')"
                  [ngModel]="displayValue(row.index, 'start', row.start)"
                  (ngModelChange)="onDraft(row.index, 'start', $event)"
                  (blur)="commit(row.index, 'start')"
                  (focus)="selectRow(row, false)"
                  spellcheck="false"
                  autocomplete="off"
                />
                <button
                  pButton
                  type="button"
                  class="p-button-rounded p-button-text p-button-sm"
                  icon="pi pi-step-forward"
                  [disabled]="!audioUrl"
                  title="Seek to start"
                  (click)="seekMs(row.startMs)"
                ></button>
                <button
                  pButton
                  type="button"
                  class="p-button-rounded p-button-text p-button-sm srt-table__mark"
                  icon="pi pi-flag"
                  [disabled]="!audioUrl"
                  title="Mark in here"
                  (click)="markInFor(row.index)"
                ></button>
              </div>
            </td>
            <td (click)="$event.stopPropagation()">
              <div class="srt-table__time-cell">
                <input
                  pInputText
                  class="srt-table__time"
                  [class.srt-table__time--invalid]="invalidField(row.index, 'end')"
                  [ngModel]="displayValue(row.index, 'end', row.end)"
                  (ngModelChange)="onDraft(row.index, 'end', $event)"
                  (blur)="commit(row.index, 'end')"
                  (focus)="selectRow(row, false)"
                  spellcheck="false"
                  autocomplete="off"
                />
                <button
                  pButton
                  type="button"
                  class="p-button-rounded p-button-text p-button-sm"
                  icon="pi pi-step-forward"
                  [disabled]="!audioUrl"
                  title="Seek to end"
                  (click)="seekMs(row.endMs)"
                ></button>
                <button
                  pButton
                  type="button"
                  class="p-button-rounded p-button-text p-button-sm srt-table__mark"
                  icon="pi pi-flag-fill"
                  [disabled]="!audioUrl"
                  title="Mark out here"
                  (click)="markOutFor(row.index)"
                ></button>
              </div>
            </td>
            <td (click)="$event.stopPropagation()">
              <textarea
                pTextarea
                class="srt-table__text"
                [autoResize]="true"
                rows="1"
                [ngModel]="displayValue(row.index, 'text', row.text)"
                (ngModelChange)="onDraft(row.index, 'text', $event)"
                (blur)="commit(row.index, 'text')"
                (focus)="selectRow(row, false)"
                spellcheck="true"
              ></textarea>
            </td>
            <td (click)="$event.stopPropagation()">
              <button
                pButton
                type="button"
                class="p-button-rounded p-button-text p-button-sm p-button-danger"
                icon="pi pi-trash"
                title="Delete cue"
                (click)="deleteCue(row.index)"
                aria-label="Delete cue"
              ></button>
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="6">
              No cues yet. Click <strong>Add cue</strong>, then use Mark In / Mark Out while audio plays.
            </td>
          </tr>
        </ng-template>
      </p-table>

      @if (lastError) {
        <p class="srt-table__error">{{ lastError }}</p>
      }
    </div>
  `,
  styles: [
    `
      .srt-check {
        display: grid;
        gap: 1rem;
      }

      .srt-check__player {
        display: grid;
        gap: 0.75rem;
        padding: 0.9rem 1rem;
        background: var(--ls-surface, #151b24);
        border-radius: 4px;
      }

      .srt-check__controls {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
        align-items: center;
      }

      .srt-check__speed {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 0.35rem;
      }

      .srt-check__speed-label {
        font-size: 0.72rem;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        color: var(--ls-muted, #9aa7b5);
        margin-right: 0.25rem;
      }

      .srt-check__speed-btn {
        min-width: 2.75rem;
        padding-left: 0.45rem;
        padding-right: 0.45rem;
      }

      .srt-check__scrub {
        display: grid;
        grid-template-columns: auto 1fr auto;
        gap: 0.75rem;
        align-items: center;
      }

      .srt-check__slider {
        width: 100%;
      }

      .srt-check__time {
        font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
        font-size: 0.8rem;
        color: var(--ls-muted, #9aa7b5);
        min-width: 5.5rem;
      }

      .srt-check__meta {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1rem;
      }

      @media (max-width: 720px) {
        .srt-check__meta {
          grid-template-columns: 1fr;
        }
      }

      .srt-check__now,
      .srt-check__selected {
        display: grid;
        gap: 0.15rem;
      }

      .srt-check__now-label {
        font-size: 0.7rem;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        color: var(--ls-muted, #9aa7b5);
      }

      .srt-check__now-text {
        font-family: 'Noto Sans Tamil', 'Noto Sans', sans-serif;
        font-size: 1.05rem;
        white-space: pre-wrap;
      }

      .srt-check__now small,
      .srt-check__selected small {
        color: var(--ls-muted, #9aa7b5);
        font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
      }

      .srt-check__shortcut {
        font-size: 0.75rem;
        color: var(--ls-muted, #9aa7b5);
      }

      .srt-check__select-hint {
        margin: 0;
        font-size: 0.85rem;
        color: var(--ls-muted, #9aa7b5);
        align-self: center;
      }

      .srt-check__missing {
        margin: 0;
        color: var(--ls-muted, #9aa7b5);
        font-size: 0.9rem;
      }

      .srt-table__row {
        cursor: pointer;
      }

      .srt-table__row--active {
        background: color-mix(in srgb, var(--ls-accent, #6b9fff) 12%, transparent) !important;
      }

      .srt-table__row--selected {
        outline: 1px solid var(--ls-accent, #6b9fff);
        outline-offset: -1px;
      }

      .srt-table__time-cell {
        display: flex;
        align-items: center;
        gap: 0.05rem;
      }

      .srt-table__time {
        width: 100%;
        min-width: 0;
        font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
        font-size: 0.78rem;
      }

      .srt-table__time--invalid {
        border-color: #e55353;
        box-shadow: 0 0 0 1px #e55353;
      }

      .srt-table__text {
        width: 100%;
        min-width: 8rem;
        font-family: 'Noto Sans Tamil', 'Noto Sans', sans-serif;
        font-size: 0.9rem;
        line-height: 1.4;
      }

      .srt-table__mark {
        opacity: 0.85;
      }

      .srt-table__error {
        margin: 0;
        color: #e55353;
        font-size: 0.88rem;
      }
    `
  ]
})
export class SrtPreviewTableComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('audio') audioRef?: ElementRef<HTMLAudioElement>;

  @Input({ required: true }) lyrics: LyricCueView[] = [];
  @Input() audioUrl = '';
  @Input() durationMs = 0;
  @Input() saving = false;
  @Output() lyricsChange = new EventEmitter<LyricCueView[]>();
  @Output() saveRequest = new EventEmitter<void>();

  readonly playing = signal(false);
  readonly currentMs = signal(0);
  readonly scrubValue = signal(0);
  readonly selectedIndex = signal<number | null>(null);
  readonly playbackRate = signal(1);

  readonly playbackRates = PLAYBACK_RATES;

  lastError = '';
  cueStopMs: number | null = null;

  private readonly invalid = new Map<string, boolean>();
  private readonly drafts = new Map<string, string>();
  private rafId = 0;
  private destroyed = false;

  @HostListener('window:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (this.isTypingTarget(event.target)) return;
    if (event.key === 'i' || event.key === 'I') {
      event.preventDefault();
      this.markIn();
    } else if (event.key === 'o' || event.key === 'O') {
      event.preventDefault();
      this.markOut();
    } else if (event.key === 'p' || event.key === 'P') {
      event.preventDefault();
      this.addCue();
    } else if (event.key === ' ' && this.audioUrl) {
      event.preventDefault();
      this.togglePlay();
    }
  }

  get effectiveDurationMs(): number {
    if (this.durationMs > 0) return this.durationMs;
    const audio = this.audioRef?.nativeElement;
    if (audio?.duration && Number.isFinite(audio.duration)) {
      return Math.round(audio.duration * 1000);
    }
    return this.lyrics.at(-1)?.endMs ?? 0;
  }

  activeCue(): LyricCueView | null {
    const ms = this.currentMs();
    return this.lyrics.find((l) => ms >= l.startMs && ms < l.endMs) ?? null;
  }

  selectedCue(): LyricCueView | null {
    const idx = this.selectedIndex();
    if (idx === null) return null;
    return this.lyrics.find((l) => l.index === idx) ?? null;
  }

  trackByIndex = (_: number, row: LyricCueView): number => row.index;

  ngAfterViewInit(): void {
    this.bindAudioSource();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['audioUrl']) {
      this.bindAudioSource();
    }
    if (changes['lyrics']) {
      const prev = changes['lyrics'].previousValue as LyricCueView[] | undefined;
      const prevLen = prev?.length ?? 0;
      if (this.lyrics.length > 0 && prevLen === 0 && this.selectedIndex() === null) {
        this.selectedIndex.set(this.lyrics[0]?.index ?? null);
      }
      const sel = this.selectedIndex();
      if (sel !== null && !this.lyrics.some((l) => l.index === sel)) {
        this.selectedIndex.set(this.lyrics[0]?.index ?? null);
      }
    }
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.stopLoop();
    this.audioRef?.nativeElement?.pause();
  }

  invalidField(index: number, field: FieldKey): boolean {
    return this.invalid.get(this.key(index, field)) === true;
  }

  displayValue(index: number, field: FieldKey, fallback: string): string {
    return this.drafts.get(this.key(index, field)) ?? fallback;
  }

  onDraft(index: number, field: FieldKey, value: string): void {
    this.drafts.set(this.key(index, field), value);
    this.invalid.delete(this.key(index, field));
    this.lastError = '';
  }

  commit(index: number, field: FieldKey): void {
    const cue = this.lyrics.find((l) => l.index === index);
    if (!cue) return;

    const draft = this.drafts.get(this.key(index, field));
    const raw = draft ?? cue[field];
    this.drafts.delete(this.key(index, field));

    if (field === 'text') {
      const updated = this.lyrics.map((c) => (c.index === index ? { ...c, text: raw } : c));
      this.emitLyrics(updated);
      return;
    }

    const trimmed = raw.trim();
    const ms = parseSrtTimecode(trimmed);
    if (ms === null) {
      this.invalid.set(this.key(index, field), true);
      this.lastError = `Invalid ${field} time on cue #${index}. Use HH:MM:SS.mmm`;
      return;
    }

    const startMs = field === 'start' ? ms : cue.startMs;
    const endMs = field === 'end' ? ms : cue.endMs;

    if (endMs <= startMs) {
      this.invalid.set(this.key(index, field), true);
      this.lastError = `Cue #${index}: end must be after start.`;
      return;
    }

    const updated = this.lyrics.map((c) =>
      c.index === index ? updateCueTimes(c, startMs, endMs) : c
    );
    this.emitLyrics(updated);
  }

  selectRow(row: LyricCueView, seek = true): void {
    this.selectedIndex.set(row.index);
    if (seek && this.audioUrl) {
      this.clearCueStop();
      this.seekMs(row.startMs);
    }
  }

  markIn(): void {
    const idx = this.selectedIndex();
    if (idx === null) return;
    this.markInFor(idx);
  }

  markOut(): void {
    const idx = this.selectedIndex();
    if (idx === null) return;
    this.markOutFor(idx);
  }

  markInFor(index: number): void {
    const cue = this.lyrics.find((l) => l.index === index);
    if (!cue) return;
    const ms = this.markMs();
    const endMs = Math.max(ms + 500, cue.endMs);
    const updated = this.lyrics.map((c) => (c.index === index ? updateCueTimes(c, ms, endMs) : c));
    this.selectedIndex.set(index);
    this.emitLyrics(updated);
  }

  markOutFor(index: number): void {
    const cue = this.lyrics.find((l) => l.index === index);
    if (!cue) return;
    const ms = this.markMs();
    if (ms <= cue.startMs) {
      this.lastError = `Cue #${index}: mark out must be after start (${cue.start}).`;
      return;
    }
    const updated = this.lyrics.map((c) => (c.index === index ? updateCueTimes(c, c.startMs, ms) : c));
    this.selectedIndex.set(index);
    this.lastError = '';
    this.emitLyrics(updated);
    this.clearCueStop();
    this.pause();
  }

  addCue(): void {
    const ms = this.currentMs();
    const next = reindexLyrics([...this.lyrics, createEmptyCue(ms)]);
    const created = next.at(-1)!;
    this.selectedIndex.set(created.index);
    this.lastError = '';
    this.drafts.delete(this.key(created.index, 'start'));
    this.drafts.delete(this.key(created.index, 'end'));
    this.drafts.delete(this.key(created.index, 'text'));
    this.emitLyrics(next);
    if (this.audioUrl) {
      this.play();
    }
  }

  deleteCue(index: number): void {
    const next = reindexLyrics(this.lyrics.filter((c) => c.index !== index));
    if (this.selectedIndex() === index) {
      this.selectedIndex.set(next[0]?.index ?? null);
    }
    this.lastError = '';
    this.emitLyrics(next);
  }

  togglePlay(): void {
    if (this.playing()) {
      this.pause();
    } else {
      this.play();
    }
  }

  play(): void {
    const audio = this.audioRef?.nativeElement;
    if (!audio || !this.audioUrl) return;
    this.cueStopMs = null;
    void audio.play().then(() => {
      this.playing.set(true);
      this.startLoop();
    });
  }

  pause(): void {
    this.audioRef?.nativeElement?.pause();
    this.playing.set(false);
    this.stopLoop();
  }

  restart(): void {
    this.clearCueStop();
    this.seekMs(0);
    this.play();
  }

  onEnded(): void {
    this.pause();
    this.clearCueStop();
  }

  onScrub(value: number | undefined): void {
    if (value == null) return;
    const ms = (value / 100) * this.effectiveDurationMs;
    this.clearCueStop();
    this.seekMs(ms);
  }

  seekMs(ms: number): void {
    const audio = this.audioRef?.nativeElement;
    if (!audio) return;
    const clamped = Math.max(0, Math.min(ms, this.effectiveDurationMs || ms));
    audio.currentTime = clamped / 1000;
    this.currentMs.set(clamped);
    this.scrubValue.set(this.effectiveDurationMs > 0 ? (clamped / this.effectiveDurationMs) * 100 : 0);
  }

  playCue(row: LyricCueView): void {
    if (!this.audioUrl) return;
    this.selectedIndex.set(row.index);
    this.cueStopMs = row.endMs;
    this.seekMs(row.startMs);
    const audio = this.audioRef?.nativeElement;
    if (!audio) return;
    void audio.play().then(() => {
      this.playing.set(true);
      this.startLoop();
    });
  }

  clearCueStop(): void {
    this.cueStopMs = null;
  }

  formatMs(ms: number): string {
    return formatSrtTimecode(Math.max(0, Math.round(ms)));
  }

  formatRate(rate: number): string {
    if (rate === 1) return '1×';
    return `${rate.toFixed(2).replace(/0$/, '')}×`;
  }

  setPlaybackRate(rate: number): void {
    this.playbackRate.set(rate);
    this.applyPlaybackRate();
  }

  private applyPlaybackRate(): void {
    const audio = this.audioRef?.nativeElement;
    if (audio) {
      audio.playbackRate = this.playbackRate();
    }
  }

  private markMs(): number {
    return Math.max(0, this.currentMs() - MARK_REACTION_OFFSET_MS);
  }

  private emitLyrics(lyrics: LyricCueView[]): void {
    this.lyricsChange.emit(lyrics);
  }

  private isTypingTarget(target: EventTarget | null): boolean {
    if (!(target instanceof HTMLElement)) return false;
    const tag = target.tagName;
    return tag === 'INPUT' || tag === 'TEXTAREA' || target.isContentEditable;
  }

  private bindAudioSource(): void {
    const audio = this.audioRef?.nativeElement;
    if (!audio) return;
    if (this.audioUrl && audio.src !== this.audioUrl) {
      audio.src = this.audioUrl;
      audio.load();
      this.currentMs.set(0);
      this.scrubValue.set(0);
      this.playing.set(false);
      this.stopLoop();
      this.clearCueStop();
    }
    this.applyPlaybackRate();
  }

  private startLoop(): void {
    this.stopLoop();
    const tick = (): void => {
      if (this.destroyed || !this.playing()) return;
      const audio = this.audioRef?.nativeElement;
      if (!audio) return;

      const ms = Math.round(audio.currentTime * 1000);
      this.currentMs.set(ms);
      this.scrubValue.set(this.effectiveDurationMs > 0 ? (ms / this.effectiveDurationMs) * 100 : 0);

      if (this.cueStopMs !== null && ms >= this.cueStopMs) {
        this.pause();
        this.seekMs(this.cueStopMs);
        this.clearCueStop();
        return;
      }

      this.rafId = requestAnimationFrame(tick);
    };
    this.rafId = requestAnimationFrame(tick);
  }

  private stopLoop(): void {
    if (this.rafId) {
      cancelAnimationFrame(this.rafId);
      this.rafId = 0;
    }
  }

  private key(index: number, field: FieldKey): string {
    return `${index}:${field}`;
  }
}
