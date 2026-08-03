using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Auth.GetBootstrap;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Identity.Auth.Queries.GetBootstrap;

public record GetBootstrapQuery : IQuery<Result<BootstrapResult>>, IGlobalRequest, IAuthenticatedRequest;

public class GetBootstrapQueryHandler : IRequestHandler<GetBootstrapQuery, Result<BootstrapResult>>
{
    private readonly IIdentityDbContext _identityContext;
    private readonly IAccountDbContext _accountContext;
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly ICurrentUser _currentUser;

    public GetBootstrapQueryHandler(
        IIdentityDbContext identityContext,
        IAccountDbContext accountContext,
        IWorkspaceDbContext workspaceContext,
        ICurrentUser currentUser)
    {
        _identityContext = identityContext;
        _accountContext = accountContext;
        _workspaceContext = workspaceContext;
        _currentUser = currentUser;
    }

    public async Task<Result<BootstrapResult>> Handle(GetBootstrapQuery request, CancellationToken cancellationToken)
    {
        var user = await _identityContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user is null)
            return Result<BootstrapResult>.Failure("User not found");

        var workspaces = await _workspaceContext.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.UserId == _currentUser.UserId)
            .Join(_workspaceContext.Workspaces,
                member => member.WorkspaceId,
                workspace => workspace.Id,
                (member, workspace) => new WorkspaceInfo
                {
                    Id = workspace.Id,
                    Name = workspace.Name,
                    Slug = workspace.Slug,
                    Role = member.Role.ToString()
                })
            .ToListAsync(cancellationToken);

        var accountMember = await _accountContext.AccountMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == _currentUser.UserId, cancellationToken);

        Guid? personalWorkspaceId = null;
        if (accountMember is not null)
        {
            var pw = await _workspaceContext.Workspaces
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.AccountId == accountMember.AccountId && w.IsPersonal, cancellationToken);
            personalWorkspaceId = pw?.Id;
        }

        return Result<BootstrapResult>.Success(new BootstrapResult
        {
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email.Value,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                EmailConfirmed = user.EmailConfirmed
            },
            Workspaces = workspaces,
            PersonalWorkspace = new PersonalWorkspaceStatus
            {
                Status = personalWorkspaceId is not null ? "ready" : "pending",
                WorkspaceId = personalWorkspaceId
            }
        });
    }
}
