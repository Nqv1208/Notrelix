using Notrelix.Application.Events.Identity;

namespace Notrelix.Infrastructure.Messaging.Consumers.Identity;

public sealed class UserDeactivatedConsumer : IConsumer<UserDeactivatedIntegrationEvent>
{
    private readonly ILogger<UserDeactivatedConsumer> _logger;

    public UserDeactivatedConsumer(ILogger<UserDeactivatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<UserDeactivatedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[Identity] UserDeactivated: UserId={UserId}",
            context.Message.UserId);
        return Task.CompletedTask;
    }
}
