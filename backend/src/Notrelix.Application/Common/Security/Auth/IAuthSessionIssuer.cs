namespace Notrelix.Application.Common.Security.Auth;

public interface IAuthSessionIssuer
{
    Task<AuthResult> IssueAsync(
        User user,
        DateTimeOffset now,
        CancellationToken ct);
}
