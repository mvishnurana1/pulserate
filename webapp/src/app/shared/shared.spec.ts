import { sparklinePoints } from './sparkline';
import { formatDuration, relativeTime } from './time.pipes';

describe('relativeTime', () => {
  const now = Date.parse('2026-11-27T10:00:00Z');

  it.each([
    ['2026-11-27T09:59:58Z', 'just now'],
    ['2026-11-27T09:59:48Z', '12s ago'],
    ['2026-11-27T09:55:00Z', '5m ago'],
    ['2026-11-27T07:00:00Z', '3h ago'],
    ['2026-11-25T10:00:00Z', '2d ago'],
    ['2026-11-27T10:00:40Z', 'in 40s'],
  ])('formats %s as "%s"', (value, expected) => {
    expect(relativeTime(value, now)).toBe(expected);
  });

  it('says "never" when there is no time', () => {
    expect(relativeTime(null, now)).toBe('never');
  });
});

describe('formatDuration', () => {
  it.each([
    [0, '0s'],
    [45, '45s'],
    [60, '1m'],
    [3725, '1h 2m'],
    [90_061, '1d 1h'],
  ])('formats %d seconds as "%s"', (seconds, expected) => {
    expect(formatDuration(seconds)).toBe(expected);
  });
});

describe('sparklinePoints', () => {
  it('returns nothing for no data', () => {
    expect(sparklinePoints([], 100, 20)).toEqual([]);
  });

  it('spreads points across the width and keeps them inside the box', () => {
    const points = sparklinePoints([10, 20, 30], 100, 20);

    expect(points.map((p) => p.x)).toEqual([2, 50, 98]);
    for (const point of points) {
      expect(point.y).toBeGreaterThanOrEqual(2);
      expect(point.y).toBeLessThanOrEqual(18);
    }
    // Higher values are drawn higher up (smaller y).
    expect(points[2].y).toBeLessThan(points[0].y);
  });

  it('handles all-zero values without dividing by zero', () => {
    const points = sparklinePoints([0, 0], 100, 20);
    expect(points.every((p) => Number.isFinite(p.y))).toBe(true);
  });
});
