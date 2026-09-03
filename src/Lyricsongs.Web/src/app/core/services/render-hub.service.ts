import { Injectable, OnDestroy, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RenderJobView } from '../models/project.models';

export interface RenderProgressEvent {
  jobId: string;
  status: string;
  progress: number;
  message?: string;
  downloadUrl?: string;
  error?: string;
}

@Injectable({ providedIn: 'root' })
export class RenderHubService implements OnDestroy {
  private connection?: signalR.HubConnection;
  private readonly progressSubject = new Subject<RenderProgressEvent>();

  readonly progress$ = this.progressSubject.asObservable();
  readonly connected = signal(false);
  readonly currentJob = signal<RenderJobView | null>(null);

  async connect(projectId: string): Promise<void> {
    if (this.connection) {
      await this.join(projectId);
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/render`)
      .withAutomaticReconnect()
      .build();

    this.connection.on('RenderStarted', (payload: RenderProgressEvent) => {
      this.applyProgress(payload, 'preparing');
    });

    this.connection.on('RenderProgress', (payload: RenderProgressEvent) => {
      this.applyProgress(payload, this.mapStatus(payload.status));
    });

    this.connection.on('RenderCompleted', (payload: RenderProgressEvent) => {
      const job = this.currentJob();
      if (job && job.id === payload.jobId) {
        this.currentJob.set({
          ...job,
          status: 'completed',
          progressPercent: 100,
          stage: payload.status,
          message: payload.message,
          outputUrl: payload.downloadUrl,
          completedAt: new Date().toISOString()
        });
      }
      this.progressSubject.next(payload);
    });

    this.connection.on('RenderFailed', (payload: RenderProgressEvent) => {
      const job = this.currentJob();
      if (job && job.id === payload.jobId) {
        const cancelled = payload.status === 'Cancelled';
        this.currentJob.set({
          ...job,
          status: cancelled ? 'cancelled' : 'failed',
          stage: payload.status,
          message: payload.message,
          errorMessage: payload.error ?? payload.message
        });
      }
      this.progressSubject.next(payload);
    });

    try {
      await this.connection.start();
      this.connected.set(true);
      await this.join(projectId);
    } catch {
      this.connected.set(false);
    }
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = undefined;
      this.connected.set(false);
    }
  }

  startLocalJob(job: RenderJobView): void {
    this.currentJob.set(job);
  }

  cancelLocalJob(): void {
    const job = this.currentJob();
    if (job) {
      this.currentJob.set({
        ...job,
        status: 'cancelled',
        stage: 'Cancelled',
        message: 'Render cancelled by user'
      });
    }
  }

  ngOnDestroy(): void {
    void this.disconnect();
  }

  private applyProgress(payload: RenderProgressEvent, status: RenderJobView['status']): void {
    this.progressSubject.next(payload);
    const job = this.currentJob();
    if (job && job.id === payload.jobId) {
      this.currentJob.set({
        ...job,
        progressPercent: payload.progress,
        stage: payload.status,
        message: payload.message,
        status
      });
    }
  }

  private mapStatus(status: string): RenderJobView['status'] {
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
        return 'rendering';
    }
  }

  private async join(projectId: string): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('JoinProjectGroup', projectId);
    }
  }
}
