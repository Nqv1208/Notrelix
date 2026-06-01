using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Document.Common;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;

namespace Notrelix.Application.Features.Document.Commands.Pages.CreatePage;

public record CreatePageCommand(
    Guid WorkspaceId,
    string Title,
    Guid? ParentId
) : IRequest<Result<Guid>>;

public class CreatePageCommandHandler : IRequestHandler<CreatePageCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreatePageCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreatePageCommand request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces.AsNoTracking()
            .AnyAsync(workspace => workspace.Id == request.WorkspaceId && !workspace.IsArchived, ct);
        if (!workspaceExists) throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var position = await _context.Pages
            .Where(page => page.WorkspaceId == request.WorkspaceId && page.ParentId == request.ParentId && !page.IsDeleted)
            .MaxAsync(page => (double?)page.Position, ct) + 1024d ?? 1024d;

        var page = Page.Create(request.WorkspaceId, _currentUser.UserId, request.Title, request.ParentId);
        page.Move(request.ParentId, position);
        _context.Pages.Add(page);
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(page.Id);
    }
}
