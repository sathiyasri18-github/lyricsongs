import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { LyricCueView } from '../../core/models/project.models';
import { createUntimedTextCue, reindexLyrics } from './lyric-cue.util';
import { parseTranscriptSegments } from './transcript-segment.util';

/** Plain lyric lines for QC (no SRT timestamps). */
export function lyricsToTranscript(lyrics: LyricCueView[]): string {
  return lyrics.map((c) => c.text).join('\n');
}

@Component({
  selector: 'app-transcript-qc',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule],
  template: `
    <section class="transcript-qc">
      <div class="transcript-qc__head">
        <div>
          <h3 class="transcript-qc__title">Raw transcript</h3>
          <p class="transcript-qc__hint">
            Paste AI transcription with a pipe (<strong>|</strong>) after each lyric line, then create table entries.
            Add start/end times in the table while audio plays.
          </p>
        </div>
        <div class="transcript-qc__actions">
          <button
            pButton
            type="button"
            label="Create cues from transcript"
            icon="pi pi-table"
            class="p-button-sm"
            [disabled]="!segmentCount"
            (click)="createCuesFromTranscript()"
          ></button>
          <button
            pButton
            type="button"
            label="Reset from cues"
            icon="pi pi-refresh"
            class="p-button-sm p-button-outlined"
            [disabled]="!lyrics.length"
            (click)="resetFromCues()"
          ></button>
        </div>
      </div>
      <textarea
        class="transcript-qc__area"
        [ngModel]="text"
        (ngModelChange)="onTextChange($event)"
        spellcheck="true"
        placeholder="Hello vanakkam | Copy the text | Next line |"
      ></textarea>
      <div class="transcript-qc__meta">
        <span>{{ segmentCount }} pipe segments · {{ lyrics.length }} table cues</span>
        @if (status) {
          <span [class.transcript-qc__status--ok]="!error" [class.transcript-qc__status--err]="error">{{ status }}</span>
        }
      </div>
    </section>
  `,
  styles: [
    `
      .transcript-qc {
        margin-top: 1.5rem;
        display: grid;
        gap: 0.75rem;
      }

      .transcript-qc__head {
        display: flex;
        justify-content: space-between;
        align-items: start;
        gap: 1rem;
        flex-wrap: wrap;
      }

      .transcript-qc__title {
        margin: 0;
        font-family: var(--ls-font-display);
        font-size: 1rem;
        font-weight: 500;
      }

      .transcript-qc__hint {
        margin: 0.25rem 0 0;
        color: var(--ls-muted);
        font-size: 0.85rem;
        max-width: 42rem;
      }

      .transcript-qc__actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
      }

      .transcript-qc__area {
        width: 100%;
        min-height: 14rem;
        resize: vertical;
        padding: 0.85rem 1rem;
        border: 1px solid var(--p-content-border-color, #3a4554);
        border-radius: 4px;
        background: var(--ls-surface, #151b24);
        color: inherit;
        font-family: 'Noto Sans Tamil', 'Noto Sans', sans-serif;
        font-size: 0.95rem;
        line-height: 1.55;
      }

      .transcript-qc__area:focus {
        outline: 2px solid var(--ls-accent, #6b9fff);
        outline-offset: 1px;
      }

      .transcript-qc__meta {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem 1.25rem;
        font-size: 0.85rem;
        color: var(--ls-muted);
      }

      .transcript-qc__status--ok {
        color: #3dd68c;
      }

      .transcript-qc__status--err {
        color: #e55353;
      }
    `
  ]
})
export class TranscriptQcComponent implements OnChanges {
  @Input({ required: true }) lyrics: LyricCueView[] = [];
  @Output() lyricsChange = new EventEmitter<LyricCueView[]>();

  text = '';
  status = '';
  error = false;
  private dirty = false;

  get segmentCount(): number {
    return parseTranscriptSegments(this.text).length;
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['lyrics'] && !this.dirty && !this.text.trim()) {
      const fromLyrics = lyricsToTranscript(this.lyrics);
      if (fromLyrics.trim()) {
        this.text = fromLyrics;
      }
    }
  }

  onTextChange(value: string): void {
    this.dirty = true;
    this.text = value;
    this.status = '';
    this.error = false;
  }

  resetFromCues(): void {
    this.dirty = false;
    this.text = lyricsToTranscript(this.lyrics);
    this.status = 'Loaded text from timed cues.';
    this.error = false;
  }

  createCuesFromTranscript(): void {
    const segments = parseTranscriptSegments(this.text);
    if (!segments.length) {
      this.status = 'Add pipe-separated lyrics first (e.g. Line one | Line two |).';
      this.error = true;
      return;
    }

    const cues = reindexLyrics(segments.map((segment) => createUntimedTextCue(segment)));
    this.error = false;
    this.status = `Created ${cues.length} table cue(s) with times at 0. Play audio and use Mark In / Mark Out to set times.`;
    this.lyricsChange.emit(cues);
  }
}
