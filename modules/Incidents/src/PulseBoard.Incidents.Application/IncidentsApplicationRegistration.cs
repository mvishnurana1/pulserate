using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using PulseBoard.SharedKernel.Messaging;

[assembly: InternalsVisibleTo("PulseBoard.Incidents.Tests")]

namespace PulseBoard.Incidents.Application;

public static class IncidentsApplicationRegistration
{
    public static IServiceCollection AddIncidentsApplication(this IServiceCollection services) =>
        services.AddHandlersFromAssembly(typeof(IncidentsApplicationRegistration).Assembly);
}
