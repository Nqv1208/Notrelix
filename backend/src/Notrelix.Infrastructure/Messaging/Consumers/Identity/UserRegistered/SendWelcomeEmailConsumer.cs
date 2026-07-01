using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Identity.Registration.Commands.SendWelcomeEmail;

namespace Notrelix.Infrastructure.Messaging.Consumers.Identity.UserRegistered;

public sealed class SendWelcomeEmailConsumer : IConsumer<UserRegisteredIntegrationEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<SendWelcomeEmailConsumer> _logger;

    public SendWelcomeEmailConsumer(
        ISender sender,
        ILogger<SendWelcomeEmailConsumer> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Consume(
        ConsumeContext<UserRegisteredIntegrationEvent> context)
    {
        var message = context.Message;

        var result = await _sender.Send(new SendWelcomeEmailCommand(
            UserId: message.UserId,
            Email: message.Email,
            DisplayName: message.DisplayName,
            MessageId: context.MessageId ?? Guid.NewGuid(),
            SourceEventId: message.SourceEventId,
            SourceMessageName: nameof(UserRegisteredIntegrationEvent),
            SourceMessageVersion: 1,
            CorrelationId: context.CorrelationId?.ToString(),
            CausationId: null,
            OccurredAt: message.OccurredAt
        ), context.CancellationToken);

        _logger.LogInformation(
            "[Identity] UserRegistered: UserId={UserId}, Email={Email}, DisplayName={DisplayName}",
            message.UserId, message.Email, message.DisplayName);

    }
}