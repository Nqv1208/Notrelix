
namespace Notrelix.Infrastructure.Email
{
    public sealed class NoopEmailService(ILogger<NoopEmailService> logger) : IEmailService
    {
        public Task<EmailDeliveryResult> SendAsync(
            EmailDeliveryRequest request,
            CancellationToken cancellationToken = default)
        {
            logger.LogInformation(
                "Email delivery disabled. Skipped email to {ToEmail} with subject {Subject}",
                request.RecipientEmail,
                request.Subject);

            return Task.FromResult(new EmailDeliveryResult("noop", request.IdempotencyKey));
        }
    }
}
