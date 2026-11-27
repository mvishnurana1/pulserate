var builder = DistributedApplication.CreateBuilder(args);

// Postgres in a container, with data kept in a named volume between runs.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

// One database, one schema per module. The name must match the modules' ConnectionName.
var database = postgres.AddDatabase("pulseboard-db");

// Azure Storage emulator (Azurite): Functions host storage plus the check-requests queue.
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator.WithDataVolume());
var queues = storage.AddQueues("queues");

// Aspire 17 adds .NET projects by path (relative to this AppHost project).
// The API applies migrations on startup in development, so everything else waits for it.
var api = builder.AddDotnetProject("api", "../../api/src/PulseBoard.Api/PulseBoard.Api.csproj")
    .WithReference(database)
    .WaitFor(database);

// Functions keep their own resource type in Aspire 17, so they get the Functions host and debugging.
builder.AddAzureFunctionsProject("functions", "../../pipeline/src/PulseBoard.Functions/PulseBoard.Functions.csproj")
    .WithHostStorage(storage)
    .WithReference(queues)
    .WithReference(database)
    .WaitFor(storage)
    .WaitFor(api);

// Angular dev server on its usual port. Its proxy reads the API's address from the variables Aspire injects.
builder.AddJavaScriptApp("web", "../../webapp", "start")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 4200, isProxied: false)
    .WithExternalHttpEndpoints();

builder.Build().Run();
