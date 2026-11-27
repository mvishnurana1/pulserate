using System.Text.Json.Serialization;
using PulseBoard.Api.Common;
using PulseBoard.Api.Realtime;
using PulseBoard.Incidents.Infrastructure;
using PulseBoard.Monitoring.Infrastructure;
using PulseBoard.SharedKernel.Messaging;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Modules
builder.AddMonitoringModule();
builder.AddMonitoringOutboxProcessor(); // The API hosts the event subscribers, so it publishes the outbox.
builder.AddIncidentsModule();

// Web
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

// Real-time: the API's own integration event handlers (the SignalR notifier).
builder.Services.AddSignalR();
builder.Services.AddHandlersFromAssembly(typeof(ChecksHub).Assembly);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Interactive API docs at /scalar
    await app.Services.MigrateMonitoringDatabaseAsync();
    await app.Services.MigrateIncidentsDatabaseAsync();
}

// In development the Angular dev server proxies plain-HTTP requests to the API. Redirecting them to HTTPS
// sends the browser to a different origin, which fails CORS. TLS is terminated at the edge in production.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();
app.MapHub<ChecksHub>(ChecksHub.Path);
app.MapDefaultEndpoints();

app.Run();
