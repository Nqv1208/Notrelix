using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Application.Features.Accounts.Public.Security;

namespace Notrelix.Application.Features.Accounts.Security.Queries.GetAccountSecurityPolicy;

/// <summary>The Account's security policy: what it requires of its members, and whether the caller may change it (an active Account Owner).</summary>
public sealed record AccountSecurityPolicyDto(bool RequireMfa, bool CanManage);

public sealed record GetAccountSecurityPolicyQuery
    : IQuery<Result<AccountSecurityPolicyDto>>, IAuthenticatedRequest, IAccountRequest, IReadRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef? Resource => null;
}

public sealed class GetAccountSecurityPolicyQueryHandler
    : IRequestHandler<GetAccountSecurityPolicyQuery, Result<AccountSecurityPolicyDto>>
{
    private readonly IAccountSecurityPolicyFacts _facts;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IAccountDbContext _context;

    public GetAccountSecurityPolicyQueryHandler(
        IAccountSecurityPolicyFacts facts, ICurrentRequestContext currentUser, IAccountDbContext context)
    {
        _facts = facts;
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result<AccountSecurityPolicyDto>> Handle(
        GetAccountSecurityPolicyQuery request, CancellationToken cancellationToken)
    {
        var accountId = _currentUser.RequireAccountId();
        var userId = _currentUser.UserId;
        var canManage = await _context.AccountMembers.AsNoTracking().AnyAsync(
            m => m.AccountId == accountId && m.UserId == userId
                && m.Status == AccountMemberStatus.Active && m.Role == AccountRole.Owner,
            cancellationToken);
        return Result<AccountSecurityPolicyDto>.Success(new AccountSecurityPolicyDto(
            await _facts.IsMfaRequiredAsync(accountId, cancellationToken), canManage));
    }
}
