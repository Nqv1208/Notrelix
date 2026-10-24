namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public sealed record CompleteMfaChallengeRequest
{
    /// <summary>
    /// Challenge returned by the sign-in step. Omitted after an OAuth sign-in: the challenge then
    /// travels in the httpOnly MFA challenge cookie. A token in the body always wins over the cookie.
    /// </summary>
    public string? ChallengeToken { get; init; }

    public string Code { get; init; } = string.Empty;
}
