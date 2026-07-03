namespace Notrelix.Application.Common.CQRS;

/// <summary>
/// Marker for account-scoped requests (no workspace).
/// </summary>
public interface IAccountRequest
{
    Guid AccountId { get; }
}
