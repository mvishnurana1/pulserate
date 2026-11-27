# PulseBoard

[![CI](https://github.com/mvishnurana1/pulserate/actions/workflows/ci.yml/badge.svg)](https://github.com/mvishnurana1/pulserate/actions/workflows/ci.yml)

A small uptime and status monitor: ASP.NET Core (.NET 10) modular monolith,
Azure Functions for scheduled checks, Angular frontend, orchestrated with Aspire.

## Layout
| Folder      | What lives there                                      |
|-------------|-------------------------------------------------------|
| `api/`      | ASP.NET Core host: controllers, SignalR, auth         |
| `modules/`  | Business modules (Domain, Application, Infrastructure, Contracts) |
| `pipeline/` | Azure Functions: thin triggers into module Application layers |
| `shared/`   | Shared kernel                                         |
| `webapp/`   | Angular app                                           |
| `aspire/`   | Local orchestration (AppHost, ServiceDefaults)        |
| `tests/`    | Cross-cutting tests (architecture rules)              |
| `docs/adr/` | Architecture decision records                         |
| `http/`     | Bruno API collection                                  |
| `infra/`    | Bicep / docker-compose                                |

## How it works
1. **Scheduling:** every 15 seconds a timer function claims the checks that are due and queues one
   message per check (Azure Storage queue).
2. **Checking:** a queue-triggered function calls the endpoint once (10 s timeout, no retries) and records
   the result. The check flips to *Down* only after its failure threshold is reached.
3. **Events:** the result, status change and domain events are saved in one transaction, with the events
   written to an outbox table ([ADR 3](docs/adr/0003-transactional-outbox.md)).
4. **Reacting:** the API publishes the outbox in-process. The Incidents module opens or resolves incidents,
   and a SignalR hub pushes live updates to the Angular dashboard.

## Run locally
Prerequisites: .NET 10 SDK, Docker Desktop (or Podman), Node.js LTS, Azure Functions Core Tools v4.

```powershell
dotnet tool restore
cd webapp; npm install; cd ..
dotnet run --project aspire/PulseBoard.AppHost
```

The Aspire dashboard starts Postgres, pgAdmin, Azurite, the API, the Functions app and the Angular dev
server. Open the dashboard at http://localhost:4200, and the API docs at `/scalar` on the API.

### Migrations
Each module has its own `DbContext`, schema and migration history:

```powershell
dotnet ef migrations add <Name> --context MonitoringDbContext -p modules/Monitoring/src/PulseBoard.Monitoring.Infrastructure -s api/src/PulseBoard.Api -o Persistence/Migrations
dotnet ef migrations add <Name> --context IncidentsDbContext -p modules/Incidents/src/PulseBoard.Incidents.Infrastructure -s api/src/PulseBoard.Api -o Persistence/Migrations
```

## Tests
```powershell
dotnet test PulseBoard.slnx              # needs Docker: integration tests start a real Postgres
cd webapp; npm test -- --watch=false
```

| Suite | What it covers |
|-------|----------------|
| `modules/*/tests` | Domain rules (thresholds, events, scheduling, incidents) and the SSRF address policy |
| `api/tests` | The real API and both modules against Postgres in Docker (Testcontainers): HTTP contracts, problem details, the outbox-to-incident flow, idempotency, scheduling |
| `tests/PulseBoard.Architecture.Tests` | Module and layer boundaries |
| `webapp` | Client state, live updates, formatting and stats (Vitest) |

CI runs all of it on every push and pull request, with warnings treated as errors.

## Security notes
- **SSRF:** the prober refuses private, loopback, link-local and reserved addresses at connection time,
  redirects included ([ADR 4](docs/adr/0004-ssrf-protection.md)). To monitor internal services on purpose, set
  `Monitoring__Prober__AllowPrivateNetworks=true` on the Functions app.
- **Authentication** is not implemented yet.

## Design decisions
See [docs/adr](docs/adr).
