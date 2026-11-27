import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { RealtimeService } from './core/realtime/realtime.service';
import { ChecksStore } from './core/state/checks.store';
import { NotificationService } from './core/state/notification.service';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="topbar">
      <a class="brand" routerLink="/">
        <span class="brand__pulse" aria-hidden="true"></span>
        PulseBoard
      </a>
      <nav class="nav" aria-label="Main">
        <a routerLink="/" routerLinkActive="nav__link--active" [routerLinkActiveOptions]="{ exact: true }" class="nav__link">Checks</a>
        <a routerLink="/incidents" routerLinkActive="nav__link--active" class="nav__link">
          Incidents
          @if (downCount() > 0) {
            <span class="nav__count">{{ downCount() }}</span>
          }
        </a>
      </nav>
      <span class="connection" [class]="'connection connection--' + realtime.status()" [title]="connectionLabel()">
        <span class="connection__dot"></span>
        <span class="hide-sm">{{ connectionLabel() }}</span>
      </span>
    </header>

    <main class="container">
      <router-outlet />
    </main>

    <div class="toasts" aria-live="polite">
      @for (toast of notifications.notifications(); track toast.id) {
        <div class="toast" [class]="'toast toast--' + toast.tone">
          <div>
            <strong>{{ toast.title }}</strong>
            @if (toast.detail) {
              <div class="small">{{ toast.detail }}</div>
            }
          </div>
          <button class="toast__close" type="button" aria-label="Dismiss" (click)="notifications.dismiss(toast.id)">×</button>
        </div>
      }
    </div>
  `,
})
export class App {
  protected readonly realtime = inject(RealtimeService);
  protected readonly notifications = inject(NotificationService);
  private readonly store = inject(ChecksStore);

  protected readonly downCount = computed(() => this.store.summary().down);

  protected readonly connectionLabel = computed(() => {
    switch (this.realtime.status()) {
      case 'connected':
        return 'Live';
      case 'connecting':
      case 'reconnecting':
        return 'Connecting…';
      default:
        return 'Offline';
    }
  });

  constructor() {
    void this.realtime.start();
  }
}
