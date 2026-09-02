using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Invitations.Services;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitationById;

public record AcceptInvitationByIdCommand(
    Guid InvitationId
) : ICommand<Result<AcceptInvitationResultDto>>,
    IWriteRequest,
    IAuthenticatedRequest,
    IGlobalRequest,
    IRequireVerifiedEmail
{
}

public class AcceptInvitationByIdCommandHandler : IRequestHandler<AcceptInvitationByIdCommand, Result<AcceptInvitationResultDto>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IInvitationAcceptanceService _acceptanceService;

    public AcceptInvitationByIdCommandHandler(
        IWorkspaceDbContext context,
        ICurrentRequestContext requestContext,
        IInvitationAcceptanceService acceptanceService)
    {
        _context = context;
        _requestContext = requestContext;
        _acceptanceService = acceptanceService;
    }

    public async Task<Result<AcceptInvitationResultDto>> Handle(
        AcceptInvitationByIdCommand request, CancellationToken ct)
    {
        var invitation = await _context.WorkspaceInvitations
            .FirstOrDefaultAsync(i => i.Id == request.InvitationId, ct);

        if (invitation is null)
            throw new NotFoundException(nameof(WorkspaceInvitation), request.InvitationId);

        return await _acceptanceService.AcceptAsync(
            invitation, _requestContext.UserId, ct);
    }
}