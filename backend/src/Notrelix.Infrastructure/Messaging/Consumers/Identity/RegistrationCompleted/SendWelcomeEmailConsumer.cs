using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Identity.Registration.Commands.SendWelcomeEmail;

namespace Notrelix.Infrastructure.Messaging.Consumers.Identity.RegistrationCompleted;

public sealed class SendWelcomeEmailConsumer : IConsumer<IdentityRegistrationCompletedIntegrationEventV1>
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
        ConsumeContext<IdentityRegistrationCompletedIntegrationEventV1> context)
    {
        var msg = context.Message;

        var result = await _sender.Send(new SendWelcomeEmailCommand(
            UserId: msg.UserId,
            Email: msg.Email,
            DisplayName: msg.DisplayName,
            MessageId: context.MessageId ?? Guid.NewGuid(),
            SourceEventId: msg.SourceEventId,
            SourceMessageName: msg.MessageName,
            SourceMessageVersion: msg.SchemaVersion,
            CorrelationId: context.CorrelationId?.ToString(),
            CausationId: null,
            OccurredAt: msg.OccurredAt
        ), context.CancellationToken);

        _logger.LogInformation(
            "[Identity] RegistrationCompleted: UserId={UserId}, Email={Email}",
            msg.UserId, msg.Email);
    }
}