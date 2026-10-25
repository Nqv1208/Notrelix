using Notrelix.Application.Common.Data.Rls;

namespace Notrelix.Application.Features.Identity.Auth.Services;

/// <summary>
/// Enters the database credential scope for the one existing user a presented credential identifies, before
/// the handler reads that user. The database derives the user from the credential (a normalized address, a
/// refresh token digest, an email-verification digest); the handler's own lookups then see exactly that
/// user's rows and nothing else. A credential that matches nobody enters no scope, so the handler's lookups
/// find nothing, exactly like an unknown credential today. A host that sets no database session context has
/// nothing to enter.
/// </summary>
public sealed class CredentialScope
{
    private static readonly SystemOperationReason SignIn =
        new("Credential", "an anonymous sign-in or recovery step identifies its user by the presented credential");

    private readonly IRlsSessionContext _rls;

    public CredentialScope(IRlsSessionContext rls)
    {
        _rls = rls;
    }

    /// <summary>The user the normalized address identifies, or null.</summary>
    public Task<Guid?> EnterByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        EnterAsync(CredentialScopeKind.NormalizedEmail, normalizedEmail, 0, cancellationToken);

    /// <summary>The owner of the session with this refresh token digest, or null.</summary>
    public Task<Guid?> EnterByRefreshTokenAsync(string refreshTokenHash, CancellationToken cancellationToken) =>
        EnterAsync(CredentialScopeKind.RefreshToken, refreshTokenHash, 0, cancellationToken);

    /// <summary>The user of the email-verification token with this digest, or null.</summary>
    public Task<Guid?> EnterByEmailVerificationAsync(string tokenHash, int hashVersion, CancellationToken cancellationToken) =>
        EnterAsync(CredentialScopeKind.EmailVerificationToken, tokenHash, hashVersion, cancellationToken);

    /// <summary>The user a linked provider identity (provider and provider subject) signs in as, or null.</summary>
    public Task<Guid?> EnterByOAuthIdentityAsync(string provider, string subject, CancellationToken cancellationToken) =>
        EnterAsync(CredentialScopeKind.OAuthIdentity, $"{provider}\u001f{subject}", 0, cancellationToken);

    /// <summary>
    /// Binds the request to the user a server-held, already validated record (an MFA challenge or an OAuth
    /// state held in a server-side store) belongs to. The user comes from that record, not from the caller;
    /// the scope is the ordinary user scope for that one user.
    /// </summary>
    public Task EnterForServerBoundUserAsync(Guid userId, CancellationToken cancellationToken) =>
        _rls.IsSessionContextEnabled ? _rls.ApplyUserScopeAsync(userId, cancellationToken) : Task.CompletedTask;

    private async Task<Guid?> EnterAsync(
        CredentialScopeKind kind, string credential, int hashVersion, CancellationToken cancellationToken)
    {
        if (!_rls.IsSessionContextEnabled || string.IsNullOrWhiteSpace(credential))
            return null;

        return await _rls.EnterCredentialScopeAsync(
            new CredentialScopeRequest(kind, credential, hashVersion, SignIn), cancellationToken);
    }
}
