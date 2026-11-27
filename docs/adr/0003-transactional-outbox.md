# 3. Transactional outbox for cross-module and real-time events

Date: 2026-11-27
Status: Accepted

## Context

When a check goes down, three things must happen: Monitoring saves the result and new status, Incidents
opens an incident, and connected dashboards update. The check runs in the Functions app, but the
subscribers (Incidents and the SignalR hub) live in the API.

Publishing straight after `SaveChanges` risks the classic dual-write problem: the process can crash between
saving and publishing, so the incident is never opened. Publishing before saving is worse, because it can
announce a change that then rolls back.

## Decision

- Domain events raised on the `Check` aggregate are converted to the module's public integration events
  (in `Monitoring.Contracts`) and written to `monitoring.OutboxMessages` **in the same transaction** as the
  change, in `MonitoringDbContext.SaveChangesAsync`.
- A background service in the API (`MonitoringOutboxProcessor`) polls the outbox every second, claims rows
  with `SELECT … FOR UPDATE SKIP LOCKED` and dispatches each event in-process to every registered
  `IIntegrationEventHandler<T>`: the Incidents module and the SignalR notifier.
- Delivery is **at least once**. Handlers are idempotent: Incidents checks for an existing open incident,
  backed by a unique partial index (`CheckId WHERE Status = 'Open'`).
- A message that keeps failing is retried up to 5 times, then parked with its last error, so one bad
  message can't block the queue.

## Consequences

- No message broker to run for the in-process case. The database is the only stateful dependency.
- Events reach subscribers about a second late, which is fine for a status dashboard.
- `SKIP LOCKED` lets the API scale out without two instances publishing the same message at once.
- If a module is split into its own service later, the processor's dispatch step can publish to a broker
  (Service Bus, RabbitMQ) instead, without changing the domain or the handlers.
