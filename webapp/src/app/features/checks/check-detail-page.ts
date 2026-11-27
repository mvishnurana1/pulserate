import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { filter, firstValueFrom } from 'rxjs';
import { ChecksApi } from '../../core/api/checks-api';
import { describeError } from '../../core/api/describe-error';
import { CheckResult } from '../../core/api/models';
import { RealtimeService } from '../../core/realtime/realtime.service';
import { ChecksStore } from '../../core/state/checks.store';
import { NotificationService } from '../../core/state/notification.service';
import { ClockService } from '../../shared/clock.service';
import { Sparkline } from '../../shared/sparkline';
import { StatusBadge } from '../../shared/status-badge';
import { DurationPipe, RelativeTimePipe } from '../../shared/time.pipes';

const HISTORY_SIZE = 100;

export interface ResultStats {
  uptimePercent: number | null;
  averageMs: number | null;
  p95Ms: number | null;
  lastError: string | null;
}

/** Uptime and latency figures for the loaded window of results (newest first). */
export function calculateStats(results: readonly CheckResult[]): ResultStats {
  if (results.length === 0) {
    return { uptimePercent: null, averageMs: null, p95Ms: null, lastError: null };
  }

  const successes = results.filter((result) => result.isSuccess).length;
  const times = results.map((result) => result.responseTimeMs).sort((a, b) => a - b);
  const p95Index = Math.min(times.length - 1, Math.ceil(times.length * 0.95) - 1);

  return {
    uptimePercent: Math.round((successes / results.length) * 1000) / 10,
    averageMs: Math.round(times.reduce((sum, time) => sum + time, 0) / times.length),
    p95Ms: times[p95Index],
    lastError: results.find((result) => !result.isSuccess)?.error ?? null,
  };
}

@Component({
  selector: 'app-check-detail-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, StatusBadge, Sparkline, RelativeTimePipe, DurationPipe],
  templateUrl: './check-detail-page.html',
})
export class CheckDetailPage implements OnInit {
  private readonly api = inject(ChecksApi);
  private readonly store = inject(ChecksStore);
  private readonly realtime = inject(RealtimeService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly clock = inject(ClockService);

  /** Route parameter, bound by withComponentInputBinding. */
  readonly id = input.required<string>();

  protected readonly results = signal<CheckResult[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly busy = signal(false);

  protected readonly check = computed(() => this.store.checks().find((check) => check.id === this.id()));
  protected readonly stats = computed(() => calculateStats(this.results()));

  // Charts read oldest to newest.
  protected readonly chartValues = computed(() => this.results().map((r) => r.responseTimeMs).reverse());
  protected readonly chartFailures = computed(() => this.results().map((r) => !r.isSuccess).reverse());

  async ngOnInit(): Promise<void> {
    this.realtime.resultRecorded
      .pipe(
        filter((event) => event.checkId === this.id()),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((event) => {
        const result: CheckResult = {
          id: event.resultId,
          checkedAt: event.occurredAt,
          isSuccess: event.isSuccess,
          statusCode: event.statusCode,
          responseTimeMs: event.responseTimeMs,
          error: event.error,
        };
        this.results.update((results) => [result, ...results].slice(0, HISTORY_SIZE));
      });

    try {
      await this.store.ensureLoaded();
      this.results.set(await firstValueFrom(this.api.results(this.id(), HISTORY_SIZE)));
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected async togglePause(): Promise<void> {
    const check = this.check();
    if (!check) {
      return;
    }
    await this.run(() => (check.status === 'Paused' ? this.store.resume(check.id) : this.store.pause(check.id)));
  }

  protected async remove(): Promise<void> {
    const check = this.check();
    if (!check || !confirm(`Delete "${check.name}" and its history?`)) {
      return;
    }
    await this.run(async () => {
      await this.store.remove(check.id);
      await this.router.navigate(['/']);
    });
  }

  private async run(action: () => Promise<unknown>): Promise<void> {
    this.busy.set(true);
    try {
      await action();
    } catch (error) {
      this.notifications.show('danger', 'Action failed', describeError(error));
    } finally {
      this.busy.set(false);
    }
  }
}
