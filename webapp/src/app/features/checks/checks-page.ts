import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { describeError } from '../../core/api/describe-error';
import { Check } from '../../core/api/models';
import { ChecksStore } from '../../core/state/checks.store';
import { NotificationService } from '../../core/state/notification.service';
import { ClockService } from '../../shared/clock.service';
import { Sparkline } from '../../shared/sparkline';
import { StatusBadge } from '../../shared/status-badge';
import { DurationPipe, RelativeTimePipe } from '../../shared/time.pipes';

@Component({
  selector: 'app-checks-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, StatusBadge, Sparkline, RelativeTimePipe, DurationPipe],
  templateUrl: './checks-page.html',
})
export class ChecksPage implements OnInit {
  protected readonly store = inject(ChecksStore);
  protected readonly clock = inject(ClockService);
  private readonly notifications = inject(NotificationService);

  /** ID of the check with a pause/resume/delete request in flight, to disable its buttons. */
  protected readonly busyId = signal<string | null>(null);

  ngOnInit(): void {
    void this.store.ensureLoaded();
  }

  protected async togglePause(check: Check): Promise<void> {
    await this.run(check, () =>
      check.status === 'Paused' ? this.store.resume(check.id) : this.store.pause(check.id),
    );
  }

  protected async remove(check: Check): Promise<void> {
    if (!confirm(`Delete "${check.name}" and its history?`)) {
      return;
    }
    await this.run(check, () => this.store.remove(check.id));
  }

  private async run(check: Check, action: () => Promise<unknown>): Promise<void> {
    this.busyId.set(check.id);
    try {
      await action();
    } catch (error) {
      this.notifications.show('danger', `Couldn't update ${check.name}`, describeError(error));
    } finally {
      this.busyId.set(null);
    }
  }
}
