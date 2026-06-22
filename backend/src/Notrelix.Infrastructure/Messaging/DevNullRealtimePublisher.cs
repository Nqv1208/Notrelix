using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.CQRS;

namespace Notrelix.Infrastructure.Messaging;

public sealed class DevNullRealtimePublisher : IRealtimePublisher
{
    public Task PublishAsync(RealtimeTopic topic, object payload, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
