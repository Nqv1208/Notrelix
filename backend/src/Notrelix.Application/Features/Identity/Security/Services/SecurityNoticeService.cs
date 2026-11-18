using System.Globalization;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Security.Abstractions;

namespace Notrelix.Application.Features.Identity.Security.Services;

/// <summary>Producer-owned <see cref="ISecurityNotice"/> over the email outbox; it runs inside the request transaction.</summary>
public sealed class SecurityNoticeService : ISecurityNotice
{
    private const int MaxDeviceLength = 200;

    private readonly IIdentityDbContext _context;
    private readonly IClientMetadata _client;
    private readonly IEmailOutboxWriter _outbox;
    private readonly ISecurityPageLink _link;
    private readonly ILogger<SecurityNoticeService> _logger;

    public SecurityNoticeService(
        IIdentityDbContext context,
        IClientMetadata client,
        IEmailOutboxWriter outbox,
        ISecurityPageLink link,
        ILogger<SecurityNoticeService>? logger = null)
    {
        _context = context;
        _client = client;
        _outbox = outbox;
        _link = link;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SecurityNoticeService>.Instance;
    }

    public async Task QueueAsync(
        Guid userId,
        SecurityNoticeKind kind,
        DateTimeOffset occurredAt,
        Guid? subjectId,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.Name, Email = u.Email.Value })
                .FirstOrDefaultAsync(cancellationToken);
            if (user is null)
            {
                return;
            }

            var (headline, detail) = Describe(kind);
            var html = EmailTemplateService.SecurityNotice(
                user.Name,
                headline,
                detail,
                occurredAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC",
                DeviceText(),
                _link.Build());

            await _outbox.QueueRenderedEmailAsync(
                new QueueRenderedEmailRequest(
                    DeduplicationKey: $"identity.security-notice:{kind}:{userId:N}:{occurredAt.UtcTicks}:{subjectId:N}",
                    RecipientEmail: user.Email,
                    RecipientName: user.Name,
                    Subject: $"{headline} — Notrelix",
                    BodyHtml: html,
                    BodyText: null,
                    WorkspaceId: null,
                    RecipientUserId: userId,
                    SourceContext: "identity",
                    TemplateKey: "identity.security-notice"),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The mail is a courtesy about a change that already happened: it never fails the change.
            _logger.LogError(ex, "Failed to queue the security notice {Kind} for {UserId}", kind, userId);
        }
    }

    private string DeviceText()
    {
        var agent = string.IsNullOrWhiteSpace(_client.UserAgent) ? null : _client.UserAgent.Trim();
        if (agent is { Length: > MaxDeviceLength })
        {
            agent = agent[..MaxDeviceLength];
        }

        var parts = new[] { agent, string.IsNullOrWhiteSpace(_client.IpAddress) ? null : _client.IpAddress.Trim() }
            .Where(p => p is not null);
        var text = string.Join(" · ", parts);
        return text.Length == 0 ? "Unknown device" : text;
    }

    private static (string Headline, string Detail) Describe(SecurityNoticeKind kind) => kind switch
    {
        SecurityNoticeKind.PasswordChanged => (
            "Password changed",
            "The password of your account was changed. All active sessions were signed out; sign in again with the new password."),
        SecurityNoticeKind.PasswordReset => (
            "Password reset",
            "The password of your account was reset with the emailed code. All active sessions were signed out."),
        SecurityNoticeKind.MfaEnabled => (
            "Two-step verification turned on",
            "Two-step verification was turned on for your account."),
        SecurityNoticeKind.MfaDisabled => (
            "Two-step verification turned off",
            "Two-step verification was turned off for your account."),
        SecurityNoticeKind.ProviderLinked => (
            "Sign-in provider linked",
            "A sign-in provider was linked to your account."),
        SecurityNoticeKind.ProviderUnlinked => (
            "Sign-in provider unlinked",
            "A sign-in provider was unlinked from your account."),
        SecurityNoticeKind.SessionRevoked => (
            "A device was signed out",
            "One device was signed out of your account."),
        SecurityNoticeKind.OtherSessionsRevoked => (
            "Other devices were signed out",
            "Every device except the one that did it was signed out of your account."),
        SecurityNoticeKind.ApiTokenIssued => (
            "A new API token was created",
            "A new API token was created under your account. It can call Notrelix on your behalf until it expires or is revoked."),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
