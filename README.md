# PulseBoard

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

## Run locally
TODO
