namespace Notrelix.Infrastructure.Email
{
    public sealed class NoopEmailService(
        ILogger<NoopEmailService> logger,
        IHostEnvironment environment) : IEmailService
    {
        public Task<EmailDeliveryResult> SendAsync(
            EmailDeliveryRequest request,
            CancellationToken cancellationToken = default)
        {
            logger.LogInformation(
                "Email delivery disabled. Skipped email with subject {Subject}",
                request.Subject);

            if (environment.IsDevelopment())
            {
                // No provider is configured on a developer machine: the text of the message
                // (the confirmation link, the invitation link) is the only way to follow it.
                logger.LogInformation(
                    "Development email to {Recipient}: {BodyText}",
                    request.RecipientEmail,
                    request.BodyText);
            }

            return Task.FromResult(new EmailDeliveryResult("noop", request.IdempotencyKey));
        }
    }
}
