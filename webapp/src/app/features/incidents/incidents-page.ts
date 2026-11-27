import { ChangeDetectionStrategy, Component, effect, inject, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { describeError } from '../../core/api/describe-error';
import { IncidentsApi } from '../../core/api/incidents-api';
import { Incident, IncidentStatus } from '../../core/api/models';
import { ChecksStore } from '../../core/state/checks.store';
import { NotificationService } from '../../core/state/notification.service';
import { ClockService } from '../../shared/clock.service';
import { StatusBadge } from '../../shared/status-badge';
import { DurationPipe, RelativeTimePipe } from '../../shared/time.pipes';

type Filter = IncidentStatus | 'All';

@Component({
  selector: 'app-incidents-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, StatusBadge, RelativeTimePipe, DurationPipe],
  templateUrl: './incidents-page.html',
})
export class IncidentsPage {
  private readonly api = inject(IncidentsApi);
  private readonly store = inject(ChecksStore);
  private readonly notifications = inject(NotificationService);
  protected readonly clock = inject(ClockService);

  protected readonly filters: Filter[] = ['Open', 'Resolved', 'All'];
  protected readonly filter = signal<Filter>('Open');
  protected readonly incidents = signal<Incident[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly resolvingId = signal<string | null>(null);

  constructor() {
    // Reload when the filter changes, or when a live event says an incident opened or closed.
    effect(() => {
      const filter = this.filter();
      this.store.incidentChanges();
      untracked(() => void this.load(filter));
    });
  }

  /** Open incidents have no end yet, so their duration counts up with the clock. */
  protected durationSeconds(incident: Incident): number {
    return incident.durationSeconds ?? Math.max(0, (this.clock.now() - new Date(incident.openedAt).getTime()) / 1000);
  }

  protected async resolve(incident: Incident): Promise<void> {
    this.resolvingId.set(incident.id);
    try {
      await firstValueFrom(this.api.resolve(incident.id));
      await this.load(this.filter());
    } catch (error) {
      this.notifications.show('danger', "Couldn't resolve the incident", describeError(error));
    } finally {
      this.resolvingId.set(null);
    }
  }

  private async load(filter: Filter): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.incidents.set(await firstValueFrom(this.api.list(filter === 'All' ? undefined : filter)));
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }
}
