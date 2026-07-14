using System.Net;
using System.Text.Json;
using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Notifications.Email;
using Notrelix.Application.Features.Notifications.WorkspaceInvitations.Abstractions;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.Notifications.Email;

public sealed record RenderedEmail(
    string Subject,
    string BodyHtml,
    string? BodyText);

public interface IEmailTemplateMaterializer
{
    string TemplateKey { get; }
    int TemplateVersion { get; }

    Task<RenderedEmail?> MaterializeAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken);
}

public interface IEmailTemplateMaterializerRegistry
{
    IEmailTemplateMaterializer? Find(string templateKey, int templateVersion);
}

public sealed class EmailTemplateMaterializerRegistry : IEmailTemplateMaterializerRegistry
{
    private readonly IReadOnlyDictionary<string, IEmailTemplateMaterializer> _materializers;

    public EmailTemplateMaterializerRegistry(
        IEnumerable<IEmailTemplateMaterializer> materializers)
    {
        _materializers = materializers.ToDictionary(
            x => Key(x.TemplateKey, x.TemplateVersion),
            StringComparer.OrdinalIgnoreCase);
    }

    public IEmailTemplateMaterializer? Find(string templateKey, int templateVersion)
        => _materializers.GetValueOrDefault(Key(templateKey, templateVersion));

    private static string Key(string templateKey, int templateVersion)
        => $"{templateKey}:{templateVersion}";
}

public sealed class WorkspaceInvitationEmailMaterializer : IEmailTemplateMaterializer
{
    private readonly ApplicationDbContext _context;
    private readonly ISecretEncryptor _secretEncryptor;
    private readonly IWorkspaceInvitationLinkBuilder _linkBuilder;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IOneTimeTokenService _tokenService;

    public WorkspaceInvitationEmailMaterializer(
        ApplicationDbContext context,
        ISecretEncryptor secretEncryptor,
        IWorkspaceInvitationLinkBuilder linkBuilder,
        IDateTimeProvider dateTimeProvider,
        IOneTimeTokenService tokenService)
    {
        _context = context;
        _secretEncryptor = secretEncryptor;
        _linkBuilder = linkBuilder;
        _dateTimeProvider = dateTimeProvider;
        _tokenService = tokenService;
    }

    public string TemplateKey => "workspace-invitation";
    public int TemplateVersion => 1;

    public async Task<RenderedEmail?> MaterializeAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken)
    {
        var payload = Deserialize<WorkspaceInvitationEmailPayload>(message.TemplateDataJson);
        if (payload is null)
            return null;

        var invitation = await _context.WorkspaceInvitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == payload.InvitationId, cancellationToken);
        var now = _dateTimeProvider.UtcNow;

        if (invitation is null
            || invitation.Status != WorkspaceInvitationStatus.Pending
            || now >= invitation.ExpiresAt
            || invitation.TokenGeneration != payload.TokenGeneration
            || !SameDatabaseInstant(invitation.ExpiresAt, payload.ExpiresAt))
        {
            return null;
        }

        string rawToken;
        try
        {
            rawToken = _secretEncryptor.Unprotect(
                payload.ProtectedToken.Value,
                OneTimeTokenProtectionPurposes.WorkspaceInvitation);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            return null;
        }

        ParsedOneTimeToken parsedToken;
        try
        {
            parsedToken = _tokenService.ParseAndHash(
                rawToken,
                TokenPurpose.WorkspaceInvitation);
        }
        catch (InvalidOneTimeTokenException)
        {
            return null;
        }

        if (parsedToken.TokenHash != invitation.Token.Value
            || parsedToken.HashVersion != invitation.HashVersion)
        {
            return null;
        }

        var link = _linkBuilder.Build(rawToken);
        var safeEmail = WebUtility.HtmlEncode(invitation.Email);
        var safeLink = WebUtility.HtmlEncode(link);
        var expires = WebUtility.HtmlEncode(invitation.ExpiresAt.ToString("MMMM dd, yyyy"));

        return new RenderedEmail(
            "You've been invited to join a workspace",
            $"""
            <p>Hi {safeEmail},</p>
            <p>You've been invited to join a workspace on Notrelix.</p>
            <p>Click the link below to accept the invitation. This link expires on <strong>{expires}</strong>.</p>
            <p><a href="{safeLink}">Accept Invitation</a></p>
            <p>If you didn't request this invitation, you can safely ignore this email.</p>
            """,
            $"You've been invited to join a workspace. Accept it here: {link}");
    }

    private static T? Deserialize<T>(JsonDocument? document)
        => document is null
            ? default
            : document.Deserialize<T>();

    private static bool SameDatabaseInstant(
        DateTimeOffset left,
        DateTimeOffset right)
    {
        const long ticksPerMicrosecond = TimeSpan.TicksPerMicrosecond;
        return left.ToUniversalTime().Ticks / ticksPerMicrosecond
            == right.ToUniversalTime().Ticks / ticksPerMicrosecond;
    }
}

public sealed class EmailVerificationEmailMaterializer : IEmailTemplateMaterializer
{
    private readonly ApplicationDbContext _context;
    private readonly ISecretEncryptor _secretEncryptor;
    private readonly IEmailVerificationLinkBuilder _linkBuilder;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IOneTimeTokenService _tokenService;

    public EmailVerificationEmailMaterializer(
        ApplicationDbContext context,
        ISecretEncryptor secretEncryptor,
        IEmailVerificationLinkBuilder linkBuilder,
        IDateTimeProvider dateTimeProvider,
        IOneTimeTokenService tokenService)
    {
        _context = context;
        _secretEncryptor = secretEncryptor;
        _linkBuilder = linkBuilder;
        _dateTimeProvider = dateTimeProvider;
        _tokenService = tokenService;
    }

    public string TemplateKey => "email-verification";
    public int TemplateVersion => 1;

    public async Task<RenderedEmail?> MaterializeAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken)
    {
        var payload = message.TemplateDataJson?.Deserialize<EmailVerificationEmailPayload>();
        if (payload is null)
            return null;

        var token = await _context.EmailVerificationTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == payload.VerificationTokenId, cancellationToken);
        var now = _dateTimeProvider.UtcNow;

        if (token is null
            || token.UserId != payload.UserId
            || token.Status != UserTokenStatus.Active
            || now >= token.ExpiresAt
            || !SameDatabaseInstant(token.ExpiresAt, payload.ExpiresAt))
        {
            return null;
        }

        string rawToken;
        try
        {
            rawToken = _secretEncryptor.Unprotect(
                payload.ProtectedToken.Value,
                OneTimeTokenProtectionPurposes.EmailVerification);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            return null;
        }

        ParsedOneTimeToken parsedToken;
        try
        {
            parsedToken = _tokenService.ParseAndHash(
                rawToken,
                TokenPurpose.EmailVerification);
        }
        catch (InvalidOneTimeTokenException)
        {
            return null;
        }

        if (parsedToken.TokenHash != token.TokenHash.Value
            || parsedToken.HashVersion != token.HashVersion)
        {
            return null;
        }

        var link = _linkBuilder.Build(rawToken);
        var safeLink = WebUtility.HtmlEncode(link);
        var safeEmail = WebUtility.HtmlEncode(message.RecipientEmail);

        return new RenderedEmail(
            "Confirm your Notrelix email address",
            $"""
            <p>Hi {safeEmail},</p>
            <p>Confirm your email address to finish setting up your Notrelix account.</p>
            <p><a href="{safeLink}">Confirm email address</a></p>
            <p>This link expires in one hour. If you did not create this account, you can ignore this message.</p>
            """,
            $"Confirm your Notrelix email address here: {link}");
    }

    private static bool SameDatabaseInstant(
        DateTimeOffset left,
        DateTimeOffset right)
    {
        const long ticksPerMicrosecond = TimeSpan.TicksPerMicrosecond;
        return left.ToUniversalTime().Ticks / ticksPerMicrosecond
            == right.ToUniversalTime().Ticks / ticksPerMicrosecond;
    }
}
