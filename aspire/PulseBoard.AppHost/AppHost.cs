var builder = DistributedApplication.CreateBuilder(args);

// Postgres in a container, with data kept in a named volume between runs.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

// The name must match MonitoringModule.ConnectionName.
var database = postgres.AddDatabase("pulseboard-db");

// Aspire 17 adds .NET projects by path (relative to this AppHost project).
builder.AddDotnetProject("api", "../../api/src/PulseBoard.Api/PulseBoard.Api.csproj")
    .WithReference(database)
    .WaitFor(database);

builder.Build().Run();
