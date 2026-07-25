using System.Net;
using System.Net.Mail;

namespace Notrelix.Infrastructure.Email
{
    public sealed class SmtpEmailService(IOptions<SmtpOptions> smtpOptions) : IEmailService
    {
        private readonly SmtpOptions _options = smtpOptions.Value;

        public async Task<EmailDeliveryResult> SendAsync(
            EmailDeliveryRequest request,
            CancellationToken cancellationToken = default)
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromEmail, _options.FromName),
                Subject = request.Subject,
                Body = request.BodyText ?? request.BodyHtml,
                IsBodyHtml = true
            };
            message.To.Add(request.RecipientEmail);
            message.Headers.Add(
                "Message-ID",
                $"<{request.IdempotencyKey}@mail.notrelix>");

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                client.Credentials = new NetworkCredential(_options.Username, _options.Password);
            }

            await client.SendMailAsync(message, cancellationToken);
            return new EmailDeliveryResult("smtp", request.IdempotencyKey);
        }
    }
}
