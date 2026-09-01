namespace Notrelix.Application.Features.Identity.Public.Facts;

/// <summary>
/// Producer-owned public fact describing an Identity user's invitation
/// eligibility, expressed in stable consumer language. Domain
/// <c>UserStatus</c> never crosses this seam.
/// </summary>
public sealed record IdentityUserFact(
    Guid UserId,
    string Email,
    bool EmailConfirmed,
    bool CanParticipate);
