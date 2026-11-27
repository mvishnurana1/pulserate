import { Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { CheckRecoveredEvent, CheckResultRecordedEvent, CheckWentDownEvent } from '../api/models';

export type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

/**
 * Owns the single SignalR connection to the API's checks hub and re-exposes server pushes as streams.
 * Components never touch SignalR directly, which keeps them easy to test.
 */
@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private readonly connection: HubConnection = new HubConnectionBuilder()
    .withUrl('/hubs/checks')
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(LogLevel.Warning)
    .build();

  readonly status = signal<ConnectionStatus>('disconnected');

  readonly resultRecorded = new Subject<CheckResultRecordedEvent>();
  readonly wentDown = new Subject<CheckWentDownEvent>();
  readonly recovered = new Subject<CheckRecoveredEvent>();

  constructor() {
    // Event names match the method names on the server's IChecksClient interface.
    this.connection.on('CheckResultRecorded', (event: CheckResultRecordedEvent) => this.resultRecorded.next(event));
    this.connection.on('CheckWentDown', (event: CheckWentDownEvent) => this.wentDown.next(event));
    this.connection.on('CheckRecovered', (event: CheckRecoveredEvent) => this.recovered.next(event));

    this.connection.onreconnecting(() => this.status.set('reconnecting'));
    this.connection.onreconnected(() => this.status.set('connected'));
    this.connection.onclose(() => this.status.set('disconnected'));
  }

  async start(): Promise<void> {
    if (this.connection.state !== HubConnectionState.Disconnected) {
      return;
    }

    this.status.set('connecting');
    try {
      await this.connection.start();
      this.status.set('connected');
    } catch {
      // The API may still be starting. Automatic reconnect only covers drops after a successful start.
      this.status.set('disconnected');
      setTimeout(() => void this.start(), 5000);
    }
  }
}
