namespace Notrelix.Application.Common.Email
{
    public interface IEmailService
    {
        Task<EmailDeliveryResult> SendAsync(
            EmailDeliveryRequest request,
            CancellationToken cancellationToken = default);

        Task<EmailDeliveryResult> SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
            => SendAsync(
                new EmailDeliveryRequest(
                    toEmail,
                    null,
                    subject,
                    htmlBody,
                    null,
                    $"legacy-{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{toEmail}:{subject}:{htmlBody}"))).ToLowerInvariant()}"),
                cancellationToken);
    }
}
