// Shapes returned by the PulseBoard API. Dates arrive as ISO 8601 strings.

export type CheckStatus = 'Pending' | 'Up' | 'Down' | 'Paused';

export interface Check {
  id: string;
  name: string;
  url: string;
  intervalSeconds: number;
  failureThreshold: number;
  status: CheckStatus;
  consecutiveFailures: number;
  createdAt: string;
  lastCheckedAt: string | null;
  nextCheckAt: string;
}

export interface CheckInput {
  name: string;
  url: string;
  intervalSeconds: number;
  failureThreshold: number;
}

export interface CheckResult {
  id: string;
  checkedAt: string;
  isSuccess: boolean;
  statusCode: number | null;
  responseTimeMs: number;
  error: string | null;
}

export type IncidentStatus = 'Open' | 'Resolved';

export interface Incident {
  id: string;
  checkId: string;
  checkName: string;
  url: string;
  status: IncidentStatus;
  openedAt: string;
  resolvedAt: string | null;
  durationSeconds: number | null;
  resolution: 'Recovered' | 'Manual' | null;
  failureCount: number;
  lastError: string | null;
}

/** RFC 9457 problem details, plus the API's error code. */
export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  code?: string;
  errors?: Record<string, string[]>;
}

// Real-time events pushed over SignalR (mirrors Monitoring's integration events).

export interface CheckResultRecordedEvent {
  eventId: string;
  occurredAt: string;
  checkId: string;
  resultId: string;
  checkName: string;
  status: CheckStatus;
  isSuccess: boolean;
  statusCode: number | null;
  responseTimeMs: number;
  error: string | null;
  consecutiveFailures: number;
}

export interface CheckWentDownEvent {
  eventId: string;
  occurredAt: string;
  checkId: string;
  checkName: string;
  url: string;
  consecutiveFailures: number;
  lastError: string | null;
}

export interface CheckRecoveredEvent {
  eventId: string;
  occurredAt: string;
  checkId: string;
  checkName: string;
}
