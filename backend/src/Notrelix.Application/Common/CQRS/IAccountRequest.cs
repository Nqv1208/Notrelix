namespace Notrelix.Application.Common.CQRS;

/// <summary>
/// Marker for account-scoped requests (no workspace).
/// </summary>
public interface IAccountRequest : IUseCaseSecurityRequirement
{
    Guid AccountId { get; }

    UseCaseSecurityKind IUseCaseSecurityRequirement.SecurityKind => UseCaseSecurityKind.AccountScoped;
}
