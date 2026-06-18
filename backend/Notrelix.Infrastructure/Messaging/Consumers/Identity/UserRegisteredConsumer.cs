using MassTransit;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Events.Identity;

namespace Notrelix.Infrastructure.Messaging.Consumers.Identity;

public sealed class UserRegisteredConsumer : IConsumer<UserRegisteredIntegrationEvent>
{
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(ILogger<UserRegisteredConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<UserRegisteredIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[Identity] UserRegistered: UserId={UserId}, Email={Email}",
            context.Message.UserId,
            context.Message.Email);
        return Task.CompletedTask;
    }
}
