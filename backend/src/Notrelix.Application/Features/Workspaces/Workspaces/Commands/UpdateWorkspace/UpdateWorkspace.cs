using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.UpdateWorkspace;

public record UpdateWorkspaceCommand(
    Guid WorkspaceId,
    string? Name,
    string? Description,
    string? IconType,
    string? IconValue,
    string? Settings
) : ICommand<Result>, ITransactionalRequest;

public class UpdateWorkspaceCommandHandler : IRequestHandler<UpdateWorkspaceCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateWorkspaceCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateWorkspaceCommand request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var now = _dateTimeProvider.UtcNow;
        if (request.Name is not null) workspace.Rename(request.Name, _currentUser.UserId, now);
        if (request.Settings is not null) workspace.UpdateSettings(WorkspaceSettings.Create(), _currentUser.UserId, now);

        return Result.Success();
    }
}
