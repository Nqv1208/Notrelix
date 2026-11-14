using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

public sealed class FormSubmissionNotificationConsumerDefinition : ConsumerDefinition<FormSubmissionNotificationConsumer>
{
    public FormSubmissionNotificationConsumerDefinition()
    {
        EndpointName = "notrelix-collab-form-submission-notification-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<FormSubmissionNotificationConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 8;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
            r.Ignore<ArgumentException>();
            r.Ignore<DomainException>();
        });
    }
}
