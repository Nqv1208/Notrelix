namespace Notrelix.Application.Features.Accounts.Abstractions;

/// <summary>Counts only: how many people and workspaces an account holds. Never a row, a name or any content.</summary>
public sealed record OperatorAccountSummary(int ActiveMembers, int Workspaces);

/// <summary>
/// The impact preview of the operator console. Implemented by Infrastructure over an owner-defined database
/// function that returns the two counts and nothing else, so the operator login needs no read access to
/// membership or workspace tables.
/// </summary>
public interface IOperatorAccountSummaryReader
{
    Task<OperatorAccountSummary> GetAsync(Guid accountId, CancellationToken cancellationToken);
}
