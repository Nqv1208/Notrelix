using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Notrelix.Infrastructure.Messaging;

public sealed class IntegrationEventBus : IIntegrationEventBus
{
    private readonly IPublishEndpoint _publishEndpoint;

    private static readonly ConcurrentDictionary<Type, Func<IntegrationEventBus, IIntegrationEvent, CancellationToken, Task>> Publishers = new();

    public IntegrationEventBus(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishAsync<T>(
        T integrationEvent,
        CancellationToken cancellationToken = default)
        where T : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (typeof(T) == typeof(IIntegrationEvent))
        {
            throw new InvalidOperationException(
                "Cannot publish base IIntegrationEvent. A concrete integration event type is required.");
        }

        return PublishTypedAsync(integrationEvent, cancellationToken);
    }

    public Task PublishAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var eventType = integrationEvent.GetType();

        if (eventType == typeof(IIntegrationEvent))
        {
            throw new InvalidOperationException(
                "Cannot publish base IIntegrationEvent. A concrete integration event type is required.");
        }

        var publisher = Publishers.GetOrAdd(eventType, CreatePublisher);

        return publisher(this, integrationEvent, cancellationToken);
    }

    private Task PublishTypedAsync<T>(
        T message,
        CancellationToken cancellationToken)
        where T : IIntegrationEvent
    {
        return _publishEndpoint.Publish(message, cancellationToken);
    }

    private static Func<IntegrationEventBus, IIntegrationEvent, CancellationToken, Task> CreatePublisher(Type eventType)
    {
        var method = typeof(IntegrationEventBus)
            .GetMethod(
                nameof(PublishTypedAsync),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?? throw new InvalidOperationException(
                $"Could not find {nameof(PublishTypedAsync)} method.");

        var genericMethod = method.MakeGenericMethod(eventType);

        var busParameter = Expression.Parameter(typeof(IntegrationEventBus), "bus");
        var messageParameter = Expression.Parameter(typeof(IIntegrationEvent), "message");
        var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        var call = Expression.Call(
            busParameter,
            genericMethod,
            Expression.Convert(messageParameter, eventType),
            cancellationTokenParameter);

        return Expression
            .Lambda<Func<IntegrationEventBus, IIntegrationEvent, CancellationToken, Task>>(
                call,
                busParameter,
                messageParameter,
                cancellationTokenParameter)
            .Compile();
    }
}
