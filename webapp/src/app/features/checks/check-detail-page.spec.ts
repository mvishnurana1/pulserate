import { CheckResult } from '../../core/api/models';
import { calculateStats } from './check-detail-page';

const result = (responseTimeMs: number, isSuccess = true, error: string | null = null): CheckResult => ({
  id: crypto.randomUUID(),
  checkedAt: '2026-11-27T00:00:00Z',
  isSuccess,
  statusCode: isSuccess ? 200 : 503,
  responseTimeMs,
  error,
});

describe('calculateStats', () => {
  it('returns empty stats when there are no results', () => {
    expect(calculateStats([])).toEqual({ uptimePercent: null, averageMs: null, p95Ms: null, lastError: null });
  });

  it('calculates uptime, average, p95 and the most recent error', () => {
    // Newest first, as the API returns them.
    const results = [
      result(100),
      result(300, false, 'HTTP 503 Service Unavailable'),
      result(200),
      result(400, false, 'Timed out after 10 seconds'),
    ];

    expect(calculateStats(results)).toEqual({
      uptimePercent: 50,
      averageMs: 250,
      p95Ms: 400,
      lastError: 'HTTP 503 Service Unavailable',
    });
  });
});
