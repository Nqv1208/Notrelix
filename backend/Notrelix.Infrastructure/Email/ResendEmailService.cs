using Notrelix.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace Notrelix.Infrastructure.Email;

public class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly EmailSettings _settings;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(IResend resend, IOptions<EmailSettings> settings, ILogger<ResendEmailService> logger)
    {
        _resend = resend;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        try
        {
            var message = new EmailMessage
            {
                From = $"{_settings.FromName} <{_settings.FromEmail}>",
                Subject = subject,
                HtmlBody = htmlBody,
            };
            message.To.Add(to);

            await _resend.EmailSendAsync(message, ct);
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
            throw;
        }
    }
}
