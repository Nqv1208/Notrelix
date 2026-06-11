using System.Net;
using System.Net.Mail;
using Notrelix.Application.Common.Abstractions;
using Microsoft.Extensions.Options;

namespace Notrelix.Infrastructure.Email
{
    public sealed class SmtpEmailService(IOptions<SmtpOptions> smtpOptions) : IEmailService
    {
        private readonly SmtpOptions _options = smtpOptions.Value;

        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromEmail, _options.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(_options.UserName))
            {
                client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
            }

            await client.SendMailAsync(message, cancellationToken);
        }
    }
}