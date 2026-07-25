using Notrelix.Infrastructure.Options;

namespace Notrelix.Infrastructure.Email;

public class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly EmailOptions _options;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(IResend resend, IOptions<EmailOptions> options, ILogger<ResendEmailService> logger)
    {
        _resend = resend;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EmailDeliveryResult> SendAsync(
        EmailDeliveryRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var message = new EmailMessage
            {
                From = $"{_options.FromName} <{_options.FromEmail}>",
                Subject = request.Subject,
                HtmlBody = request.BodyHtml,
            };
            message.To.Add(request.RecipientEmail);

            var providerMessageId = await _resend.EmailSendAsync(
                request.IdempotencyKey,
                message,
                ct);
            _logger.LogInformation("Email sent to {To}: {Subject}", request.RecipientEmail, request.Subject);
            return new EmailDeliveryResult("resend", providerMessageId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", request.RecipientEmail, request.Subject);
            throw;
        }
    }
}
