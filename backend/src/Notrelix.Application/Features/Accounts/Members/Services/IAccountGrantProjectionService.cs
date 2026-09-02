using Notrelix.Domain.Accounts.Members;

namespace Notrelix.Application.Features.Accounts.Members.Services;

/// <summary>
/// Accounts-owned grant projection seam. Keeps the authz.access_grants
/// projection synchronized with Account membership changes inside the same
/// transaction as the membership mutation. Role vocabulary belongs to the
/// Accounts context — Common never carries AccountRole.
/// </summary>
public interface IAccountGrantProjectionService
{
    Task SyncAccountMemberGrantAsync(
        Guid accountId,
        Guid userId,
        AccountRole role,
        DateTimeOffset now,
        CancellationToken ct);
}
