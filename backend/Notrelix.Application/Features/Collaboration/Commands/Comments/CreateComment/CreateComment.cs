using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Shared.Commands.Comments.CreateComment;

public record CreateCommentCommand(string ResourceType, Guid ResourceId, string ContentMd, Guid? ParentCommentId) : IRequest<Result<Guid>>;

public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateCommentCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        var resourceType = Enum.Parse<ResourceType>(request.ResourceType, ignoreCase: true);

        Guid workspaceId;
        if (resourceType == ResourceType.BoardItem)
        {
            var card = await _context.BoardItems.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.ResourceId, ct);
            if (card is null) throw new NotFoundException("BoardItem", request.ResourceId);
            var list = await _context.BoardGroups.AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == card.GroupId, ct);
            var board = await _context.Boards.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == list!.BoardId, ct);
            workspaceId = board!.WorkspaceId;
        }
        else
        {
            var page = await _context.Pages.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.ResourceId, ct);
            if (page is null) throw new NotFoundException("Page", request.ResourceId);
            workspaceId = page.WorkspaceId;
        }

        var target = ResourceRef.Create(resourceType, request.ResourceId, workspaceId);
        var comment = Comment.Create(workspaceId, target, request.ContentMd, _currentUser.UserId, _dateTimeProvider.UtcNow, parentId: request.ParentCommentId);

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(comment.Id);
    }
}
