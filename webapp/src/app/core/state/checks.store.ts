import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';
import { ChecksApi } from '../api/checks-api';
import { describeError } from '../api/describe-error';
import { Check, CheckInput, CheckResultRecordedEvent, CheckStatus } from '../api/models';
import { RealtimeService } from '../realtime/realtime.service';
import { NotificationService } from './notification.service';

/** How many recent response times to keep per check for the sparklines. */
export const LATENCY_HISTORY = 30;

export interface ChecksSummary {
  total: number;
  up: number;
  down: number;
  paused: number;
  pending: number;
  averageResponseMs: number | null;
}

/**
 * Single source of truth for checks on the client. REST loads the initial state; SignalR keeps it live.
 * State is held in signals, so components re-render only when the data they read changes.
 */
@Injectable({ providedIn: 'root' })
export class ChecksStore {
  private readonly api = inject(ChecksApi);
  private readonly realtime = inject(RealtimeService);
  private readonly notifications = inject(NotificationService);

  private readonly checksState = signal<Check[]>([]);
  private readonly latencyState = signal<Record<string, number[]>>({});
  private loaded = false;

  readonly checks = this.checksState.asReadonly();
  readonly latency = this.latencyState.asReadonly();
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  /** Increments whenever an incident may have opened or closed, so incident views know to refresh. */
  readonly incidentChanges = signal(0);

  readonly summary = computed<ChecksSummary>(() => {
    const checks = this.checksState();
    const count = (status: CheckStatus) => checks.filter((check) => check.status === status).length;

    const latest = Object.values(this.latencyState())
      .map((history) => history.at(-1))
      .filter((value): value is number => value !== undefined);

    return {
      total: checks.length,
      up: count('Up'),
      down: count('Down'),
      paused: count('Paused'),
      pending: count('Pending'),
      averageResponseMs: latest.length
        ? Math.round(latest.reduce((sum, value) => sum + value, 0) / latest.length)
        : null,
    };
  });

  constructor() {
    this.realtime.resultRecorded.pipe(takeUntilDestroyed()).subscribe((event) => this.applyResult(event));

    this.realtime.wentDown.pipe(takeUntilDestroyed()).subscribe((event) => {
      this.patch(event.checkId, { status: 'Down' });
      this.incidentChanges.update((n) => n + 1);
      this.notifications.show('danger', `${event.checkName} is down`, event.lastError ?? undefined);
    });

    this.realtime.recovered.pipe(takeUntilDestroyed()).subscribe((event) => {
      this.patch(event.checkId, { status: 'Up' });
      this.incidentChanges.update((n) => n + 1);
      this.notifications.show('success', `${event.checkName} has recovered`);
    });
  }

  /** Loads once per session. Live updates keep the data fresh after that. */
  async ensureLoaded(): Promise<void> {
    if (!this.loaded) {
      await this.load();
    }
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const checks = await firstValueFrom(this.api.list());
      this.checksState.set(checks);
      this.loaded = true;
      await this.seedLatency(checks);
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  find(id: string): Check | undefined {
    return this.checksState().find((check) => check.id === id);
  }

  async create(input: CheckInput): Promise<Check> {
    const created = await firstValueFrom(this.api.create(input));
    this.checksState.update((checks) => sortByName([...checks, created]));
    return created;
  }

  async update(id: string, input: CheckInput): Promise<Check> {
    return this.replace(await firstValueFrom(this.api.update(id, input)));
  }

  async pause(id: string): Promise<Check> {
    return this.replace(await firstValueFrom(this.api.pause(id)));
  }

  async resume(id: string): Promise<Check> {
    return this.replace(await firstValueFrom(this.api.resume(id)));
  }

  async remove(id: string): Promise<void> {
    await firstValueFrom(this.api.delete(id));
    this.checksState.update((checks) => checks.filter((check) => check.id !== id));
    this.latencyState.update(({ [id]: _removed, ...rest }) => rest);
  }

  /** Applies a live result: new status, failure count and a point on the sparkline. */
  applyResult(event: CheckResultRecordedEvent): void {
    this.patch(event.checkId, {
      status: event.status,
      consecutiveFailures: event.consecutiveFailures,
      lastCheckedAt: event.occurredAt,
    });

    this.latencyState.update((all) => ({
      ...all,
      [event.checkId]: [...(all[event.checkId] ?? []), event.responseTimeMs].slice(-LATENCY_HISTORY),
    }));
  }

  private replace(updated: Check): Check {
    this.checksState.update((checks) => checks.map((check) => (check.id === updated.id ? updated : check)));
    return updated;
  }

  private patch(id: string, changes: Partial<Check>): void {
    this.checksState.update((checks) =>
      checks.map((check) => (check.id === id ? { ...check, ...changes } : check)),
    );
  }

  /** Fills each sparkline with recent history so the dashboard isn't empty until the next tick. */
  private async seedLatency(checks: Check[]): Promise<void> {
    const histories = await Promise.all(
      checks.map(async (check) => {
        try {
          const results = await firstValueFrom(this.api.results(check.id, LATENCY_HISTORY));
          return [check.id, results.map((result) => result.responseTimeMs).reverse()] as const;
        } catch {
          return [check.id, []] as const;
        }
      }),
    );

    this.latencyState.set(Object.fromEntries(histories));
  }
}

function sortByName(checks: Check[]): Check[] {
  return checks.sort((a, b) => a.name.localeCompare(b.name));
}
