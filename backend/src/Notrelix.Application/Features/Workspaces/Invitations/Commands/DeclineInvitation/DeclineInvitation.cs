using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.DeclineInvitation;

public record DeclineInvitationCommand(
    Guid InvitationId
) : ICommand<Result>, ITransactionalRequest, IAuthenticatedRequest, IGlobalRequest
{
}

public class DeclineInvitationCommandHandler : IRequestHandler<DeclineInvitationCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeclineInvitationCommandHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeclineInvitationCommand request, CancellationToken ct)
    {
        var invitation = await _context.WorkspaceInvitations
            .FirstOrDefaultAsync(i => i.Id == request.InvitationId, ct);

        if (invitation is null)
            throw new NotFoundException(nameof(WorkspaceInvitation), request.InvitationId);

        invitation.Decline(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
