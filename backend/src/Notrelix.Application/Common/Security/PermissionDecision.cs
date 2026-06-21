
namespace Notrelix.Application.Common.Security;

public sealed record PermissionDecision(
    bool IsAllowed,
    string? ReasonCode = null,
    PermissionLevel? EffectiveLevel = null
);
