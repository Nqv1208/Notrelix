namespace Notrelix.Application.Features.Accounts.Public.Facts;

/// <summary>
/// Stable Accounts-owned result of the personal Account provisioning action.
/// Crosses the Identity → Accounts public seam; carries only the resulting
/// AccountId so the caller can reference the created Account.
/// </summary>
public sealed record PersonalAccountProvisioningResult(Guid AccountId);
