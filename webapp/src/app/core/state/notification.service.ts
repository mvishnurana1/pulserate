import { Injectable, signal } from '@angular/core';

export type NotificationTone = 'success' | 'danger' | 'info';

export interface Notification {
  id: number;
  tone: NotificationTone;
  title: string;
  detail?: string;
}

/** Short-lived toasts, for live alerts like "API is down". */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private nextId = 1;
  private readonly items = signal<Notification[]>([]);

  readonly notifications = this.items.asReadonly();

  show(tone: NotificationTone, title: string, detail?: string, durationMs = 6000): void {
    const notification: Notification = { id: this.nextId++, tone, title, detail };
    this.items.update((items) => [...items, notification].slice(-4));
    setTimeout(() => this.dismiss(notification.id), durationMs);
  }

  dismiss(id: number): void {
    this.items.update((items) => items.filter((item) => item.id !== id));
  }
}
