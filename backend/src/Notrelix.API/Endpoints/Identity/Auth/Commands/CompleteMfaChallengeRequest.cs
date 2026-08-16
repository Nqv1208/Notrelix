namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public sealed record CompleteMfaChallengeRequest
{
    public string ChallengeToken { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}