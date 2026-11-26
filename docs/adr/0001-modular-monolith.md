# 1. Modular monolith with a separate Functions host

Date: 2026-11-26
Status: Accepted

## Context

PulseBoard has a few distinct areas (monitoring endpoints, managing incidents) that change for different
reasons. Microservices would give hard boundaries, but at the cost of network calls, distributed
transactions and several deployments for a small system.

## Decision

- One codebase, organised into **modules** under `modules/`. Each module has `Domain`, `Application`,
  `Infrastructure` and `Contracts` projects.
- Each module owns its data: its own `DbContext` and Postgres schema (`monitoring`, `incidents`). No
  module reads another module's tables.
- Modules talk to each other only through the other module's `Contracts` project (public interfaces and
  integration events).
- Two hosts plug the modules in: the ASP.NET Core API (HTTP) and the Azure Functions app (timers, queues).
  Both call the same `Application` layer, so business logic exists once.
- `tests/PulseBoard.Architecture.Tests` enforces these rules, so the build fails if a boundary is crossed.

## Consequences

- Simple to run and deploy, with in-process calls between modules.
- A module could be split out into its own service later, because its boundary already exists.
- Modules can't join across each other's tables. Cross-module reads go through contracts, or a module keeps
  its own copy of the data it needs, updated by events.
