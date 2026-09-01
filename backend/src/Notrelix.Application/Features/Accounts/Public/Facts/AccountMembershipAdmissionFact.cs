namespace Notrelix.Application.Features.Accounts.Public.Facts;

/// <summary>
/// Producer-owned public fact describing whether an account can admit a
/// member right now, expressed in stable consumer language. Domain
/// <c>AccountStatus</c> never crosses this seam.
/// </summary>
public sealed record AccountMembershipAdmissionFact(
    bool CanAdmitMember);
