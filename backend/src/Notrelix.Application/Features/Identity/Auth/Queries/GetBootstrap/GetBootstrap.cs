using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Auth.GetBootstrap;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Identity.Auth.Queries.GetBootstrap;

public record GetBootstrapQuery(Guid UserId) : IQuery<Result<BootstrapResult>>;

public class GetBootstrapQueryHandler : IRequestHandler<GetBootstrapQuery, Result<BootstrapResult>>
{
    private readonly IIdentityDbContext _identityContext;
    private readonly IWorkspaceDbContext _workspaceContext;

    public GetBootstrapQueryHandler(IIdentityDbContext identityContext, IWorkspaceDbContext workspaceContext)
    {
        _identityContext = identityContext;
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

        var personalWorkspace = await _workspaceContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.CreatedBy == request.UserId && w.IsPersonal, cancellationToken);

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
                Status = personalWorkspace is not null ? "ready" : "pending",
                WorkspaceId = personalWorkspace?.Id
            }
        });
    }
}
