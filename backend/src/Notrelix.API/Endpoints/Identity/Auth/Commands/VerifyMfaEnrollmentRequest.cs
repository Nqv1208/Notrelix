namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public sealed record VerifyMfaEnrollmentRequest
{
    public Guid MfaMethodId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string StepUpToken { get; init; } = string.Empty;
}