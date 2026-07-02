using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;

public record CreateWorkspaceCommand(
    string Name,
    string? Description,
    bool IsPersonal
) : ICommand<Result<Guid>>, ITransactionalRequest;

public class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, Result<Guid>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenantContext _tenant;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateWorkspaceCommandHandler(IWorkspaceDbContext context, ICurrentUser currentUser, ICurrentTenantContext tenant, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _tenant = tenant;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateWorkspaceCommand request, CancellationToken ct)
    {
        var slug = Slug.GenerateFromName(request.Name);
        var slugExists = await _context.Workspaces
            .AnyAsync(w => w.Slug == slug.Value, ct);

        var finalSlug = slugExists
            ? slug.Value + "-" + Guid.NewGuid().ToString("N")[..6]
            : slug.Value;

        var creationResult = WorkspaceFactory.CreateWithOwner(
            _tenant.RequireAccountId(),
            _currentUser.UserId, request.Name, finalSlug,
            _dateTimeProvider.UtcNow, request.IsPersonal,
            request.Description);

        _context.Workspaces.Add(creationResult.Workspace);
        _context.WorkspaceMembers.Add(creationResult.OwnerMember);

        return Result<Guid>.Success(creationResult.Workspace.Id);
    }
}
