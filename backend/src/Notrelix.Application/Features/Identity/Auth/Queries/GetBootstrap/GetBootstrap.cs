using Notrelix.Application.Common.Context;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Common.Security.Auth;
using Notrelix.Application.Features.Identity.Auth.GetBootstrap;

namespace Notrelix.Application.Features.Identity.Auth.Queries.GetBootstrap;

public record GetBootstrapQuery : IQuery<Result<BootstrapResult>>, IGlobalRequest, IAuthenticatedRequest;

public class GetBootstrapQueryHandler : IRequestHandler<GetBootstrapQuery, Result<BootstrapResult>>
{
    private readonly IIdentityBootstrapReadPort _bootstrapReadPort;
    private readonly ICurrentUser _currentUser;

    public GetBootstrapQueryHandler(
        IIdentityBootstrapReadPort bootstrapReadPort,
        ICurrentUser currentUser)
    {
        _bootstrapReadPort = bootstrapReadPort;
        _currentUser = currentUser;
    }

    public async Task<Result<BootstrapResult>> Handle(GetBootstrapQuery request, CancellationToken cancellationToken)
    {
        var projection = await _bootstrapReadPort.GetAsync(_currentUser.UserId, cancellationToken);

        if (projection is null)
            return Result<BootstrapResult>.Failure("User not found");

        return Result<BootstrapResult>.Success(new BootstrapResult
        {
            User = new UserDto
            {
                Id = projection.User.Id,
                Email = projection.User.Email,
                Name = projection.User.Name,
                AvatarUrl = projection.User.AvatarUrl,
                EmailConfirmed = projection.User.EmailConfirmed
            },
            Workspaces = projection.Workspaces
                .Select(workspace => new WorkspaceInfo
                {
                    Id = workspace.Id,
                    Name = workspace.Name,
                    Slug = workspace.Slug,
                    Role = workspace.Role
                })
                .ToList(),
            PersonalWorkspace = new PersonalWorkspaceStatus
            {
                Status = projection.PersonalWorkspaceId is not null ? "ready" : "pending",
                WorkspaceId = projection.PersonalWorkspaceId
            }
        });
    }
}
