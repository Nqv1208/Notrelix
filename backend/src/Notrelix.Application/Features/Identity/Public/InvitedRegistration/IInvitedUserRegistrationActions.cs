namespace Notrelix.Application.Features.Identity.Public.InvitedRegistration;

/// <summary>
/// Producer-owned public action: Identity creates the sign-in identity of a person who holds a
/// one-time Workspace invitation and has no account yet. Owning context: Identity; the caller
/// (Workspaces invitation acceptance) owns the business transaction and the action runs inside it,
/// so the new User, its personal Account and the invitation acceptance commit or roll back together.
/// </summary>
public interface IInvitedUserRegistrationActions
{
    /// <summary>
    /// Registers a user for <paramref name="email"/> with a password credential, marks the email
    /// confirmed (the one-time invitation token was delivered to that address, which proves control of
    /// it) and issues a session. Returns null when an identity already exists for the email, in which
    /// case nothing is created.
    /// </summary>
    Task<InvitedUserRegistration?> RegisterAsync(
        string email,
        string name,
        string password,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

/// <summary>
/// The identity created for an invited person and the session issued for it. The caller delivers the
/// tokens only as httpOnly cookies; they never reach a response body.
/// </summary>
public sealed record InvitedUserRegistration(
    Guid UserId,
    string Email,
    string Name,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);
