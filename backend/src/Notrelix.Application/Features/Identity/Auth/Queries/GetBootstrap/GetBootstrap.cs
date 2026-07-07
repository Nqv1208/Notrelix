using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.CQRS.Scoping;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Auth.GetBootstrap;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Identity.Auth.Queries.GetBootstrap;

public record GetBootstrapQuery(Guid UserId) : IQuery<Result<BootstrapResult>>, IGlobalRequest;

public class GetBootstrapQueryHandler : IRequestHandler<GetBootstrapQuery, Result<BootstrapResult>>
{
    private readonly IIdentityDbContext _identityContext;
    private readonly IAccountDbContext _accountContext;
    private readonly IWorkspaceDbContext _workspaceContext;

    public GetBootstrapQueryHandler(
        IIdentityDbContext identityContext,
        IAccountDbContext accountContext,
        IWorkspaceDbContext workspaceContext)
    {
        _identityContext = identityContext;
        _accountContext = accountContext;
        _workspaceContext = workspaceContext;
    }

    public async Task<Result<BootstrapResult>> Handle(GetBootstrapQuery request, CancellationToken cancellationToken)
    {
        var user = await _identityContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result<BootstrapResult>.Failure("User not found");

        var workspaces = await _workspaceContext.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.UserId == request.UserId)
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
            .FirstOrDefaultAsync(m => m.UserId == request.UserId, cancellationToken);

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
                AvatarUrl = user.AvatarUrl
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
