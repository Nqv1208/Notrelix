namespace Notrelix.Infrastructure.Messaging.Consumers.Identity.EmailVerificationDelivery;

public sealed class SendEmailVerificationEmailConsumerDefinition
    : ConsumerDefinition<SendEmailVerificationEmailConsumer>
{
    public SendEmailVerificationEmailConsumerDefinition()
    {
        EndpointName = "identity-email-verification-delivery";
    }
}
