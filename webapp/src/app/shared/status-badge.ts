import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CheckStatus, IncidentStatus } from '../core/api/models';

const tones: Record<CheckStatus | IncidentStatus, string> = {
  Up: 'success',
  Down: 'danger',
  Pending: 'neutral',
  Paused: 'muted',
  Open: 'danger',
  Resolved: 'success',
};

@Component({
  selector: 'app-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="badge" [class]="'badge badge--' + tone()"><span class="badge__dot"></span>{{ status() }}</span>`,
})
export class StatusBadge {
  readonly status = input.required<CheckStatus | IncidentStatus>();
  protected readonly tone = computed(() => tones[this.status()]);
}
