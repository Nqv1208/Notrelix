using Notrelix.Application.Common.Models;
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
    private readonly IAccountSecurityManageAuthorizer _manage;

    public GetAccountSecurityPolicyQueryHandler(
        IAccountSecurityPolicyFacts facts, ICurrentRequestContext currentUser, IAccountSecurityManageAuthorizer manage)
    {
        _facts = facts;
        _currentUser = currentUser;
        _manage = manage;
    }

    public async Task<Result<AccountSecurityPolicyDto>> Handle(
        GetAccountSecurityPolicyQuery request, CancellationToken cancellationToken)
    {
        var accountId = _currentUser.RequireAccountId();
        return Result<AccountSecurityPolicyDto>.Success(new AccountSecurityPolicyDto(
            await _facts.IsMfaRequiredAsync(accountId, cancellationToken),
            await _manage.CanManageAsync(cancellationToken)));
    }
}
