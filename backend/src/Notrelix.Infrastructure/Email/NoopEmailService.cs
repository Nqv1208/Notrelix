using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Infrastructure.Email
{
    public sealed class NoopEmailService(ILogger<NoopEmailService> logger) : IEmailService
    {
        public Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            logger.LogInformation(
                "Email delivery disabled. Skipped email to {ToEmail} with subject {Subject}",
                toEmail,
                subject);

            return Task.CompletedTask;
        }
    }
}
