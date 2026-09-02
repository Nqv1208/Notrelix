using Notrelix.Application.Features.Accounts.Members.Services;
using Notrelix.Domain.Accounts.Members;

namespace Notrelix.Infrastructure.Data.Authz;

public sealed class AccountGrantProjectionServiceAdapter : IAccountGrantProjectionService
{
    private readonly AccessGrantProjectionService _projection;

    public AccountGrantProjectionServiceAdapter(AccessGrantProjectionService projection)
    {
        _projection = projection;
    }

    public Task SyncAccountMemberGrantAsync(
        Guid accountId,
        Guid userId,
        AccountRole role,
        DateTimeOffset now,
        CancellationToken ct)
        => _projection.SyncAccountMemberGrantAsync(accountId, userId, role, now, ct);
}
