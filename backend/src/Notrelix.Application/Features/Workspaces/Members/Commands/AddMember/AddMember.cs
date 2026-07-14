using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.AddMember;

public record AddMemberCommand(
    Guid WorkspaceId,
    Guid UserId,
    WorkspaceRole Role
) : ICommand<Result>, ITransactionalRequest, IWorkspaceRequest, IRequirePermission, IRequireVerifiedEmail
{
    public PermissionAction Action => PermissionAction.InviteMember;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Workspace, WorkspaceId, WorkspaceId);
}

public class AddMemberCommandHandler : IRequestHandler<AddMemberCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddMemberCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(AddMemberCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && !w.IsDeleted, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var existingMember = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == request.WorkspaceId && m.UserId == request.UserId, ct);

        if (existingMember is not null)
        {
            if (existingMember.Status == WorkspaceMemberStatus.Active)
                throw new BusinessRuleException("User is already a member of this workspace.");

            existingMember.Activate(_requestContext.UserId, _dateTimeProvider.UtcNow);
            existingMember.ChangeRole(request.Role, _requestContext.UserId, 1, _dateTimeProvider.UtcNow);
        }
        else
        {
            var member = WorkspaceMember.Create(
                workspace.AccountId,
                request.WorkspaceId,
                request.UserId,
                request.Role,
                _requestContext.UserId,
                _dateTimeProvider.UtcNow);

            _context.WorkspaceMembers.Add(member);
        }

        return Result.Success();
    }
}
