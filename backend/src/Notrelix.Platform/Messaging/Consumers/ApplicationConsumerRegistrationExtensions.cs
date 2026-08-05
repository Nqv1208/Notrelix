using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Requests;
using Notrelix.Platform.Messaging.Runtime;

namespace Notrelix.Platform.Messaging.Consumers;

/// <summary>
/// Typed Application message-consumer registration (spec 3.4).
///
/// Every delivery:
/// 1. creates a fresh DI scope;
/// 2. sets the idempotency execution key to <c>EventEnvelope.Id.ToString("N")</c>;
/// 3. marks the source <see cref="IdempotencyExecutionSource.Message"/>;
/// 4. resolves <see cref="ISender"/> inside the scope;
/// 5. dispatches the command produced by the factory.
///
/// The dispatched command then flows through the same Application pipeline —
/// including <c>IdempotencyBehavior</c> — as the HTTP path, so the message source
/// never reimplements idempotency mechanics or derives keys from business data.
/// </summary>
public static class ApplicationConsumerRegistrationExtensions
{
    public static IServiceCollection AddApplicationConsumer<TCommand, TResponse>(
        this IServiceCollection services,
        string eventName,
        Func<EventEnvelope, TCommand> commandFactory,
        Action<ConsumerOptions>? configure = null)
        where TCommand : IRequest<TResponse>, IIdempotentRequest
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(commandFactory);

        return services.AddScopedConsumer(
            eventName,
            async (provider, envelope, cancellationToken) =>
            {
                using var scope = provider.CreateScope();
                var scopedProvider = scope.ServiceProvider;

                scopedProvider
                    .GetRequiredService<IIdempotencyExecutionContextWriter>()
                    .Set(envelope.Id.ToString("N"), IdempotencyExecutionSource.Message);

                var command = commandFactory(envelope);

                await scopedProvider
                    .GetRequiredService<ISender>()
                    .Send(command, cancellationToken);
            },
            configure);
    }
}
