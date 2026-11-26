using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using PulseBoard.SharedKernel.Messaging;

[assembly: InternalsVisibleTo("PulseBoard.Monitoring.Tests")]

namespace PulseBoard.Monitoring.Application;

public static class MonitoringApplicationRegistration
{
    public static IServiceCollection AddMonitoringApplication(this IServiceCollection services) =>
        services.AddHandlersFromAssembly(typeof(MonitoringApplicationRegistration).Assembly);
}
