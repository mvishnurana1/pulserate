import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { Subject } from 'rxjs';
import { signal } from '@angular/core';
import { ChecksApi } from '../api/checks-api';
import { Check, CheckRecoveredEvent, CheckResultRecordedEvent, CheckWentDownEvent } from '../api/models';
import { RealtimeService } from '../realtime/realtime.service';
import { ChecksStore, LATENCY_HISTORY } from './checks.store';
import { NotificationService } from './notification.service';

const check = (overrides: Partial<Check> = {}): Check => ({
  id: 'c1',
  name: 'Example',
  url: 'https://example.com/',
  intervalSeconds: 60,
  failureThreshold: 3,
  status: 'Up',
  consecutiveFailures: 0,
  createdAt: '2026-11-27T00:00:00Z',
  lastCheckedAt: null,
  nextCheckAt: '2026-11-27T00:01:00Z',
  ...overrides,
});

const result = (overrides: Partial<CheckResultRecordedEvent> = {}): CheckResultRecordedEvent => ({
  eventId: 'e1',
  occurredAt: '2026-11-27T00:02:00Z',
  checkId: 'c1',
  resultId: 'r1',
  checkName: 'Example',
  status: 'Up',
  isSuccess: true,
  statusCode: 200,
  responseTimeMs: 120,
  error: null,
  consecutiveFailures: 0,
  ...overrides,
});

describe('ChecksStore', () => {
  let realtime: {
    status: ReturnType<typeof signal>;
    resultRecorded: Subject<CheckResultRecordedEvent>;
    wentDown: Subject<CheckWentDownEvent>;
    recovered: Subject<CheckRecoveredEvent>;
  };
  let store: ChecksStore;

  beforeEach(async () => {
    realtime = {
      status: signal('connected'),
      resultRecorded: new Subject(),
      wentDown: new Subject(),
      recovered: new Subject(),
    };

    const api: Partial<ChecksApi> = {
      list: () => of([check(), check({ id: 'c2', name: 'Docs', status: 'Paused' })]),
      results: () => of([]),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: ChecksApi, useValue: api },
        { provide: RealtimeService, useValue: realtime },
      ],
    });

    store = TestBed.inject(ChecksStore);
    await store.load();
  });

  it('loads checks and summarises them', () => {
    expect(store.checks().length).toBe(2);
    expect(store.summary()).toMatchObject({ total: 2, up: 1, paused: 1, down: 0 });
  });

  it('applies a live result to the check and its sparkline', () => {
    realtime.resultRecorded.next(result({ status: 'Up', consecutiveFailures: 1, responseTimeMs: 250 }));

    expect(store.find('c1')).toMatchObject({ consecutiveFailures: 1, lastCheckedAt: '2026-11-27T00:02:00Z' });
    expect(store.latency()['c1']).toEqual([250]);
    expect(store.summary().averageResponseMs).toBe(250);
  });

  it(`keeps only the last ${LATENCY_HISTORY} response times`, () => {
    for (let i = 0; i < LATENCY_HISTORY + 5; i++) {
      store.applyResult(result({ responseTimeMs: i }));
    }

    expect(store.latency()['c1'].length).toBe(LATENCY_HISTORY);
    expect(store.latency()['c1'].at(-1)).toBe(LATENCY_HISTORY + 4);
  });

  it('marks a check down, raises an alert and signals incident views', () => {
    const notifications = TestBed.inject(NotificationService);
    const before = store.incidentChanges();

    realtime.wentDown.next({
      eventId: 'e2',
      occurredAt: '2026-11-27T00:03:00Z',
      checkId: 'c1',
      checkName: 'Example',
      url: 'https://example.com/',
      consecutiveFailures: 3,
      lastError: 'HTTP 503',
    });

    expect(store.find('c1')?.status).toBe('Down');
    expect(store.summary().down).toBe(1);
    expect(store.incidentChanges()).toBe(before + 1);
    expect(notifications.notifications()[0]).toMatchObject({ tone: 'danger', title: 'Example is down' });
  });
});
