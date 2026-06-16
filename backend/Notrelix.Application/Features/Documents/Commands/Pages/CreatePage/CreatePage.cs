using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Domain.SharedKernel;

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
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreatePageCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreatePageCommand request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces.AsNoTracking()
            .AnyAsync(workspace => workspace.Id == request.WorkspaceId && workspace.Status == WorkspaceStatus.Active && !workspace.IsDeleted, ct);
        if (!workspaceExists) throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var page = Page.Create(request.WorkspaceId, request.Title, _currentUser.UserId, _dateTimeProvider.UtcNow, request.ParentId);
        _context.Pages.Add(page);
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(page.Id);
    }
}
