import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export interface SparklinePoint {
  x: number;
  y: number;
}

/** Scales values into an SVG box. The max is padded so a flat line sits mid-height instead of at the top. */
export function sparklinePoints(values: readonly number[], width: number, height: number, padding = 2): SparklinePoint[] {
  if (values.length === 0) {
    return [];
  }

  const max = Math.max(...values) * 1.15 || 1;
  const step = values.length > 1 ? (width - padding * 2) / (values.length - 1) : 0;

  return values.map((value, index) => ({
    x: round(padding + index * step),
    y: round(height - padding - (value / max) * (height - padding * 2)),
  }));
}

function round(value: number): number {
  return Math.round(value * 10) / 10;
}

/** Tiny dependency-free response-time chart. Optional failure flags draw red markers. */
@Component({
  selector: 'app-sparkline',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg
      class="sparkline"
      [attr.viewBox]="'0 0 ' + width() + ' ' + height()"
      [attr.width]="width()"
      [attr.height]="height()"
      preserveAspectRatio="none"
      role="img"
      [attr.aria-label]="label()"
    >
      @if (points().length > 1) {
        <path class="sparkline__area" [attr.d]="areaPath()" />
        <path class="sparkline__line" [attr.d]="linePath()" />
      }
      @for (marker of failureMarkers(); track $index) {
        <circle class="sparkline__failure" [attr.cx]="marker.x" [attr.cy]="marker.y" r="2.5" />
      }
      @if (points().length === 0) {
        <line class="sparkline__empty" x1="0" [attr.x2]="width()" [attr.y1]="height() / 2" [attr.y2]="height() / 2" />
      }
    </svg>
  `,
})
export class Sparkline {
  readonly values = input<readonly number[]>([]);
  readonly failures = input<readonly boolean[]>([]);
  readonly width = input(120);
  readonly height = input(32);

  protected readonly points = computed(() => sparklinePoints(this.values(), this.width(), this.height()));

  protected readonly linePath = computed(() =>
    this.points()
      .map((point, index) => `${index === 0 ? 'M' : 'L'}${point.x},${point.y}`)
      .join(' '),
  );

  protected readonly areaPath = computed(() => {
    const points = this.points();
    const bottom = this.height();
    return `${this.linePath()} L${points.at(-1)!.x},${bottom} L${points[0].x},${bottom} Z`;
  });

  protected readonly failureMarkers = computed(() =>
    this.points().filter((_, index) => this.failures()[index]),
  );

  protected readonly label = computed(() => {
    const values = this.values();
    return values.length ? `Response time, latest ${values.at(-1)} ms` : 'No data yet';
  });
}
