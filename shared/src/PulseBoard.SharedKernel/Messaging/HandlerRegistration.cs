using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PulseBoard.SharedKernel.Messaging;

public static class HandlerRegistration
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
        typeof(IIntegrationEventHandler<>),
    ];

    /// <summary>
    /// Registers every command, query and integration event handler in <paramref name="assembly"/> as a scoped service.
    /// Handlers can stay internal to their module; callers depend only on the handler interfaces.
    /// </summary>
    public static IServiceCollection AddHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var registrations = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(contract => contract.IsGenericType && HandlerInterfaces.Contains(contract.GetGenericTypeDefinition()))
                .Select(contract => (Service: contract, Implementation: type)));

        foreach (var (service, implementation) in registrations)
        {
            services.AddScoped(service, implementation);
        }

        services.TryAddScoped<IntegrationEventDispatcher>();
        return services;
    }
}
