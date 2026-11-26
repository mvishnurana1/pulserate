# 2. Application layer uses EF Core directly, with no repositories

Date: 2026-11-26
Status: Accepted

## Context

A common Clean Architecture pattern puts a repository interface in front of every aggregate so the
Application layer never references EF Core. EF Core's `DbContext` is already a unit of work, and each
`DbSet` is already a repository.

## Decision

- Each module's `Application` project references `Microsoft.EntityFrameworkCore` (the provider-neutral
  core package only) and depends on a module-specific interface such as `IMonitoringDbContext`.
- Handlers use LINQ directly: tracked entities for commands, `AsNoTracking` with projections for queries.
- Provider details (Npgsql, connection strings, migrations) stay in `Infrastructure`.
- Business rules live on the domain entities, not in handlers, so the Domain project stays free of EF Core.

## Consequences

- Less code, and queries can be shaped exactly for each use case without growing a repository interface.
- Handlers can't be unit tested with simple fakes. They're covered by integration tests against a real
  Postgres (Testcontainers) instead, which also catches mapping and query translation bugs that fakes hide.
- The architecture tests still stop Application from referencing Infrastructure, Npgsql or ASP.NET Core.
