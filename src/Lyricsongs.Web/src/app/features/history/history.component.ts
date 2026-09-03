import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { ApiService } from '../../core/services/api.service';
import { RenderJobView } from '../../core/models/project.models';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [CommonModule, RouterLink, TableModule, TagModule, ButtonModule],
  template: `
    <section class="page">
      <header class="page__head">
        <div>
          <p class="eyebrow">Archive</p>
          <h1>Render History</h1>
        </div>
        <a routerLink="/render" pButton label="New render" icon="pi pi-play"></a>
      </header>

      <p-table [value]="jobs()" [paginator]="true" [rows]="10" styleClass="p-datatable-sm">
        <ng-template pTemplate="header">
          <tr>
            <th>Project</th>
            <th>Status</th>
            <th>Progress</th>
            <th>Created</th>
            <th>Completed</th>
            <th></th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-row>
          <tr>
            <td>{{ row.projectName }}</td>
            <td><p-tag [value]="row.status" [severity]="severity(row.status)" /></td>
            <td>{{ row.progressPercent }}%</td>
            <td>{{ row.createdAt | date: 'medium' }}</td>
            <td>{{ row.completedAt ? (row.completedAt | date: 'medium') : '—' }}</td>
            <td class="text-right">
              @if (row.status === 'completed') {
                <button pButton type="button" class="p-button-text" label="Download" icon="pi pi-download" (click)="download(row)"></button>
              } @else if (row.errorMessage) {
                <span class="err" [title]="row.errorMessage">Error</span>
              }
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr><td colspan="6">No renders yet.</td></tr>
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

      .err {
        color: #f0a0a0;
        font-size: 0.85rem;
      }
    `
  ]
})
export class HistoryComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly jobs = signal<RenderJobView[]>([]);

  ngOnInit(): void {
    this.api.getRenderHistory().subscribe((jobs) => this.jobs.set(jobs));
  }

  download(job: RenderJobView): void {
    const url = job.outputUrl || this.api.downloadUrl(job.id);
    window.open(url, '_blank');
  }

  severity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (status) {
      case 'completed':
        return 'success';
      case 'preparing':
      case 'rendering':
      case 'queued':
        return 'info';
      case 'failed':
        return 'danger';
      case 'cancelled':
        return 'warn';
      default:
        return 'secondary';
    }
  }
}
