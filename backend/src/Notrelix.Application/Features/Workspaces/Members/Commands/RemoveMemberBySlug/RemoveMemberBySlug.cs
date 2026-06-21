using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.RemoveMemberBySlug;

public record RemoveMemberBySlugCommand(
    string Slug,
    Guid UserId
) : ICommand<Result>, ITransactionalRequest;

public class RemoveMemberBySlugCommandHandler : IRequestHandler<RemoveMemberBySlugCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RemoveMemberBySlugCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RemoveMemberBySlugCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Slug == request.Slug, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.Slug);

        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspace.Id && m.UserId == request.UserId, ct);

        if (member is null)
            throw new NotFoundException("WorkspaceMember", request.UserId);

        var activeOwnerCount = await _context.WorkspaceMembers
            .CountAsync(m => m.WorkspaceId == workspace.Id && m.Role == WorkspaceRole.Owner && m.Status == WorkspaceMemberStatus.Active, ct);

        member.Remove(activeOwnerCount, _currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
