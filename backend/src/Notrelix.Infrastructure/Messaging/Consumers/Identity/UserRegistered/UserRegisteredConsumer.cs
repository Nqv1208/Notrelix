using Notrelix.Application.Events.Identity;

namespace Notrelix.Infrastructure.Messaging.Consumers.Identity.UserCreated;

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
            "[Identity] UserRegistered: UserId={UserId}",
            context.Message.UserId);
        return Task.CompletedTask;
    }
}
