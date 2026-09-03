import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TableModule } from 'primeng/table';
import { MessageService } from 'primeng/api';
import { ApiService } from '../../core/services/api.service';
import { ProjectSyncService } from '../../core/services/project-sync.service';
import { ProjectStateService } from '../../core/services/project-state.service';
import { ProjectSummary } from '../../core/models/project.models';
import { signal } from '@angular/core';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, RouterLink, ButtonModule, TagModule, TableModule],
  template: `
    <section class="page">
      <header class="page__head">
        <div>
          <p class="eyebrow">Library</p>
          <h1>Projects</h1>
        </div>
        <a routerLink="/new" pButton label="New Project" icon="pi pi-plus"></a>
      </header>

      <p-table [value]="projects()" [paginator]="true" [rows]="8" styleClass="p-datatable-sm">
        <ng-template pTemplate="header">
          <tr>
            <th>Name</th>
            <th>Audio</th>
            <th>Lyrics</th>
            <th>Assets</th>
            <th>Status</th>
            <th></th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-row>
          <tr>
            <td>{{ row.name }}</td>
            <td>{{ row.audioFileName || '—' }}</td>
            <td>{{ row.lyricCount ?? '—' }}</td>
            <td>{{ row.assetCount ?? '—' }}</td>
            <td>
              <p-tag [value]="row.status" [severity]="statusSeverity(row.status)" />
            </td>
            <td class="text-right actions">
              <button pButton type="button" class="p-button-text" label="Open" (click)="open(row)"></button>
              <button
                pButton
                type="button"
                class="p-button-text p-button-danger"
                icon="pi pi-trash"
                label="Delete"
                [disabled]="deletingId() === row.id"
                (click)="deleteProject(row, $event)"
              ></button>
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="6">No projects yet. Create your first lyric video.</td>
          </tr>
        </ng-template>
      </p-table>
    </section>
  `,
  styles: [
    `
      .page__head {
        display: flex;
        justify-content: space-between;
        align-items: end;
        margin-bottom: 1.5rem;
        gap: 1rem;
      }

      .page__head h1 {
        margin: 0.15rem 0 0;
        font-family: var(--ls-font-display);
        font-size: 2.4rem;
        font-weight: 500;
      }

      .eyebrow {
        margin: 0;
        letter-spacing: 0.16em;
        text-transform: uppercase;
        font-size: 0.72rem;
        color: var(--ls-accent);
      }

      .text-right {
        text-align: right;
      }

      .actions {
        display: flex;
        justify-content: flex-end;
        gap: 0.25rem;
        flex-wrap: wrap;
      }
    `
  ]
})
export class ProjectsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly sync = inject(ProjectSyncService);
  private readonly state = inject(ProjectStateService);
  private readonly router = inject(Router);
  private readonly messages = inject(MessageService);

  readonly projects = signal<ProjectSummary[]>([]);
  readonly deletingId = signal<string | null>(null);
  loading = false;

  ngOnInit(): void {
    this.api.getProjects().subscribe((list) => this.projects.set(list));
  }

  async open(row: ProjectSummary): Promise<void> {
    this.loading = true;
    try {
      await this.sync.loadProject(row.id);
      void this.router.navigate(['/editor']);
    } catch (err) {
      this.messages.add({
        severity: 'error',
        summary: 'Could not open project',
        detail: String(err)
      });
    } finally {
      this.loading = false;
    }
  }

  deleteProject(row: ProjectSummary, event: Event): void {
    event.stopPropagation();
    const ok = confirm(
      `Delete "${row.name}"?\n\nThis removes the project, uploaded files, and render history. This cannot be undone.`
    );
    if (!ok) return;

    this.deletingId.set(row.id);
    this.api.deleteProject(row.id).subscribe({
      next: () => {
        this.projects.update((list) => list.filter((p) => p.id !== row.id));
        if (this.state.project()?.id === row.id) {
          this.state.clear();
        }
        this.messages.add({
          severity: 'success',
          summary: 'Project deleted',
          detail: row.name
        });
        this.deletingId.set(null);
      },
      error: (err) => {
        this.messages.add({
          severity: 'error',
          summary: 'Delete failed',
          detail: String(err?.error ?? err?.message ?? err)
        });
        this.deletingId.set(null);
      }
    });
  }

  statusSeverity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (status) {
      case 'completed':
        return 'success';
      case 'rendering':
        return 'info';
      case 'failed':
        return 'danger';
      case 'ready':
        return 'warn';
      default:
        return 'secondary';
    }
  }
}
