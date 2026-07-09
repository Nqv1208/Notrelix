namespace Notrelix.Application.Common.Requests;

/// <summary>
/// Marker for account-scoped requests (no workspace).
/// Metadata-only — AccountId is resolved from tenant context, not from request.
/// </summary>
public interface IAccountRequest : IUseCaseSecurityRequirement
{
    UseCaseSecurityKind IUseCaseSecurityRequirement.SecurityKind => UseCaseSecurityKind.AccountScoped;
}
