namespace Notrelix.Application.Features.Identity.Mfa.DTOs;

public sealed record MfaEnrollmentStartResult(
    Guid MfaMethodId,
    string Secret,
    string OtpAuthUri);
