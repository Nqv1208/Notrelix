namespace Notrelix.Application.Common.Messaging;

/// <summary>
/// Sends integration commands to the outbox for reliable delivery.
/// Commands are routed to the owning bounded context's consumer.
/// </summary>
public interface IIntegrationCommandSender
{
    Task SendAsync<T>(T command, CancellationToken cancellationToken = default)
        where T : class, IIntegrationCommand;
}