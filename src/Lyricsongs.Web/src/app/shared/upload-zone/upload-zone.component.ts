import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FileUploadModule } from 'primeng/fileupload';
import { ProgressBarModule } from 'primeng/progressbar';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-upload-zone',
  standalone: true,
  imports: [CommonModule, FileUploadModule, ProgressBarModule],
  template: `
    <div
      class="upload-zone"
      [class.upload-zone--active]="dragging"
      [class.upload-zone--filled]="fileName"
      (dragover)="onDragOver($event)"
      (dragleave)="dragging = false"
      (drop)="onDrop($event)"
    >
      <input
        #fileInput
        type="file"
        class="upload-zone__input"
        [accept]="accept"
        [multiple]="multiple"
        (change)="onFileInput($event)"
      />

      <div class="upload-zone__icon" aria-hidden="true">
        <i [class]="icon"></i>
      </div>

      <div class="upload-zone__copy">
        <p class="upload-zone__label">{{ label }}</p>
        <p class="upload-zone__hint">{{ hint }}</p>
        @if (fileName) {
          <p class="upload-zone__file">{{ fileName }}</p>
        }
      </div>

      <button type="button" class="upload-zone__btn" (click)="fileInput.click()">
        {{ fileName ? 'Replace' : 'Browse' }}
      </button>

      @if (progress > 0 && progress < 100) {
        <p-progressBar [value]="progress" [showValue]="true" styleClass="upload-zone__progress" />
      }
    </div>
  `,
  styles: [
    `
      .upload-zone {
        position: relative;
        display: grid;
        grid-template-columns: auto 1fr auto;
        gap: 1rem;
        align-items: center;
        padding: 1.25rem 1.5rem;
        border: 1px dashed color-mix(in srgb, var(--ls-accent) 45%, transparent);
        border-radius: 4px;
        background: color-mix(in srgb, var(--ls-surface) 80%, transparent);
        transition: border-color 0.2s ease, background 0.2s ease, transform 0.2s ease;
      }

      .upload-zone--active {
        border-color: var(--ls-accent);
        background: color-mix(in srgb, var(--ls-accent) 12%, var(--ls-surface));
        transform: translateY(-1px);
      }

      .upload-zone--filled {
        border-style: solid;
        border-color: color-mix(in srgb, var(--ls-accent) 55%, transparent);
      }

      .upload-zone__input {
        display: none;
      }

      .upload-zone__icon {
        width: 2.75rem;
        height: 2.75rem;
        display: grid;
        place-items: center;
        border-radius: 50%;
        background: color-mix(in srgb, var(--ls-accent) 18%, transparent);
        color: var(--ls-accent);
        font-size: 1.15rem;
      }

      .upload-zone__label {
        margin: 0;
        font-family: var(--ls-font-display);
        font-size: 1.15rem;
        color: var(--ls-text);
      }

      .upload-zone__hint,
      .upload-zone__file {
        margin: 0.2rem 0 0;
        font-size: 0.85rem;
        color: var(--ls-muted);
      }

      .upload-zone__file {
        color: var(--ls-accent);
      }

      .upload-zone__btn {
        border: 1px solid color-mix(in srgb, var(--ls-accent) 50%, transparent);
        background: transparent;
        color: var(--ls-accent);
        padding: 0.55rem 1rem;
        border-radius: 2px;
        cursor: pointer;
        font: inherit;
        letter-spacing: 0.04em;
        text-transform: uppercase;
        font-size: 0.75rem;
      }

      .upload-zone__btn:hover {
        background: color-mix(in srgb, var(--ls-accent) 15%, transparent);
      }

      :host ::ng-deep .upload-zone__progress {
        grid-column: 1 / -1;
        height: 0.4rem;
      }

      @media (max-width: 720px) {
        .upload-zone {
          grid-template-columns: 1fr;
          text-align: center;
          justify-items: center;
        }
      }
    `
  ]
})
export class UploadZoneComponent {
  @Input() label = 'Upload';
  @Input() hint = 'Drag & drop or browse';
  @Input() accept = '*/*';
  @Input() icon = 'pi pi-upload';
  @Input() multiple = false;
  @Input() fileName = '';
  @Input() progress = 0;

  @Output() filesSelected = new EventEmitter<File[]>();

  dragging = false;
  private readonly messages = inject(MessageService);

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragging = true;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging = false;
    const files = Array.from(event.dataTransfer?.files ?? []);
    this.emitFiles(files);
  }

  onFileInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    this.emitFiles(files);
    input.value = '';
  }

  private emitFiles(files: File[]): void {
    if (!files.length) return;
    if (!this.multiple && files.length > 1) {
      this.messages.add({
        severity: 'warn',
        summary: 'Single file',
        detail: 'Only one file is accepted here.'
      });
      this.filesSelected.emit([files[0]]);
      return;
    }
    this.filesSelected.emit(files);
  }
}
