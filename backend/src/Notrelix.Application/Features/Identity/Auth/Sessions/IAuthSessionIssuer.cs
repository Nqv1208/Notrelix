namespace Notrelix.Application.Features.Identity.Auth.Sessions;

public interface IAuthSessionIssuer
{
    Task<AuthResult> IssueAsync(
        User user,
        DateTimeOffset now,
        CancellationToken ct);
}
