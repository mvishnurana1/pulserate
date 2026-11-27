using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;

namespace PulseBoard.SharedKernel.Messaging;

/// <summary>
/// An event one module publishes for others. Lives in the publishing module's Contracts project.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Unique per event, so handlers can detect redelivery.</summary>
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }
}

/// <summary>
/// Reacts to an integration event. Delivery is at-least-once, so handlers must be idempotent.
/// </summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Sends an integration event to every registered handler for its type, in-process.
/// </summary>
public sealed class IntegrationEventDispatcher(IServiceProvider services)
{
    private const string HandleMethodName = nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync);

    public async Task DispatchAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(integrationEvent.GetType());
        var handleMethod = handlerType.GetMethod(HandleMethodName)!;

        foreach (var handler in services.GetServices(handlerType))
        {
            try
            {
                await (Task)handleMethod.Invoke(handler, [integrationEvent, cancellationToken])!;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Throw(ex.InnerException);
            }
        }
    }
}
