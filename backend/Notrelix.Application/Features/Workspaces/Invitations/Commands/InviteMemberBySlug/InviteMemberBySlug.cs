using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Domain.SharedKernel;
using global::Notrelix.Domain.Workspaces.Invitations;
using global::Notrelix.Domain.Workspaces.Members;

namespace Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMemberBySlug;

public record InviteMemberBySlugCommand(
    string Slug,
    string Email,
    string Role
) : ICommand<Result<Guid>>;

public class InviteMemberBySlugCommandHandler : IRequestHandler<InviteMemberBySlugCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public InviteMemberBySlugCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(InviteMemberBySlugCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Slug == request.Slug, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.Slug);

        var now = _dateTimeProvider.UtcNow;
        var role = Enum.Parse<WorkspaceRole>(request.Role, ignoreCase: true);
        var token = InvitationTokenHash.Create(Guid.NewGuid().ToString("N"));
        var invitation = WorkspaceInvitation.Create(workspace.Id, request.Email.Trim().ToLowerInvariant(), role, token, _currentUser.UserId, now);

        _context.WorkspaceInvitations.Add(invitation);
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(invitation.Id);
    }
}
