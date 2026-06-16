using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;

public record CreateWorkspaceCommand(
    string Name,
    string? Description,
    bool IsPersonal
) : IRequest<Result<Guid>>;

public class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateWorkspaceCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
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

        var workspace = Workspace.Create(_currentUser.UserId, request.Name, finalSlug, _dateTimeProvider.UtcNow, isPersonal: request.IsPersonal);

        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(workspace.Id);
    }
}
