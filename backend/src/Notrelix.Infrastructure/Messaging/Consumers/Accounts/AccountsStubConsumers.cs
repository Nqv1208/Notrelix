using Notrelix.Application.Events.Accounts;

namespace Notrelix.Infrastructure.Messaging.Consumers.Accounts;

public sealed class AccountCreatedConsumer : IConsumer<AccountCreatedIntegrationEvent>
{
    private readonly ILogger<AccountCreatedConsumer> _logger;

    public AccountCreatedConsumer(ILogger<AccountCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<AccountCreatedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[Accounts] AccountCreated: AccountId={AccountId}, OwnerUserId={OwnerUserId}, Name={Name}",
            context.Message.AccountId,
            context.Message.OwnerUserId,
            context.Message.Name);
        return Task.CompletedTask;
    }
}
