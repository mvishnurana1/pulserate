import { Pipe, PipeTransform } from '@angular/core';

/** "just now", "12s ago", "5m ago", "3h ago", "2d ago", or "in 40s" for future times. */
export function relativeTime(value: string | null | undefined, now: number): string {
  if (!value) {
    return 'never';
  }

  const seconds = Math.round((now - new Date(value).getTime()) / 1000);
  const future = seconds < 0;
  const abs = Math.abs(seconds);

  if (abs < 5) {
    return future ? 'now' : 'just now';
  }

  const text = formatDuration(abs, 1);
  return future ? `in ${text}` : `${text} ago`;
}

/** Formats a number of seconds with its largest units, e.g. 3725 → "1h 2m". */
export function formatDuration(totalSeconds: number, parts = 2): string {
  const units: [label: string, seconds: number][] = [
    ['d', 86_400],
    ['h', 3_600],
    ['m', 60],
    ['s', 1],
  ];

  let remaining = Math.max(0, Math.round(totalSeconds));
  const pieces: string[] = [];

  for (const [label, size] of units) {
    if (pieces.length === parts) {
      break;
    }
    const amount = Math.floor(remaining / size);
    if (amount > 0 || (label === 's' && pieces.length === 0)) {
      pieces.push(`${amount}${label}`);
      remaining -= amount * size;
    }
  }

  return pieces.join(' ');
}

/** Pure pipe: pass the clock's `now()` so the output refreshes every tick. */
@Pipe({ name: 'relativeTime' })
export class RelativeTimePipe implements PipeTransform {
  transform(value: string | null | undefined, now: number): string {
    return relativeTime(value, now);
  }
}

@Pipe({ name: 'duration' })
export class DurationPipe implements PipeTransform {
  transform(seconds: number | null | undefined): string {
    return seconds == null ? '—' : formatDuration(seconds);
  }
}
