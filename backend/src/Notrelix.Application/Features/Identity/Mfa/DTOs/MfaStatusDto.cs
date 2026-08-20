namespace Notrelix.Application.Features.Identity.Mfa.DTOs;

public sealed record MfaStatusDto(
    bool IsEnabled,
    string? PrimaryMethod,
    bool HasRecoveryCodes,
    int RecoveryCodesRemaining);
