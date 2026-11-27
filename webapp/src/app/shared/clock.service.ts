import { DestroyRef, Injectable, inject, signal } from '@angular/core';

/**
 * A ticking "now" signal, so relative times ("12s ago") and open-incident durations update live
 * without each component running its own timer.
 */
@Injectable({ providedIn: 'root' })
export class ClockService {
  readonly now = signal(Date.now());

  constructor() {
    const timer = setInterval(() => this.now.set(Date.now()), 1000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }
}
