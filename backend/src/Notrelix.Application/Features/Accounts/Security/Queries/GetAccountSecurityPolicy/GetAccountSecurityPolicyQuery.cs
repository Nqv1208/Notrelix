using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Accounts.Public.Security;

namespace Notrelix.Application.Features.Accounts.Security.Queries.GetAccountSecurityPolicy;

/// <summary>The Account's security policy: what it requires of its members.</summary>
public sealed record AccountSecurityPolicyDto(bool RequireMfa);

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

    public GetAccountSecurityPolicyQueryHandler(IAccountSecurityPolicyFacts facts, ICurrentRequestContext currentUser)
    {
        _facts = facts;
        _currentUser = currentUser;
    }

    public async Task<Result<AccountSecurityPolicyDto>> Handle(
        GetAccountSecurityPolicyQuery request, CancellationToken cancellationToken) =>
        Result<AccountSecurityPolicyDto>.Success(new AccountSecurityPolicyDto(
            await _facts.IsMfaRequiredAsync(_currentUser.RequireAccountId(), cancellationToken)));
}
