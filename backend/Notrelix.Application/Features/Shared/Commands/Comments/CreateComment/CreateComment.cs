using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Shared.Comments.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Shared.Commands.Comments.CreateComment;

public record CreateCommentCommand(string ResourceType, Guid ResourceId, string ContentMd, Guid? ParentCommentId) : IRequest<Result<Guid>>;

public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateCommentCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        var resourceType = Enum.Parse<ResourceType>(request.ResourceType, ignoreCase: true);

        // Resolve workspaceId from the resource
        Guid workspaceId;
        if (resourceType == Domain.Enums.ResourceType.Card)
        {
            var card = await _context.Cards.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.ResourceId, ct);
            if (card is null) throw new NotFoundException("Card", request.ResourceId);

            var list = await _context.BoardLists.AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == card.ListId, ct);
            var board = await _context.Boards.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == list!.BoardId, ct);
            workspaceId = board!.WorkspaceId;
        }
        else // Page
        {
            var page = await _context.Pages.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.ResourceId, ct);
            if (page is null) throw new NotFoundException("Page", request.ResourceId);
            workspaceId = page.WorkspaceId;
        }

        var comment = Comment.Create(workspaceId, resourceType, request.ResourceId, _currentUser.UserId, request.ContentMd);

        if (request.ParentCommentId.HasValue)
        {
            // Validate parent exists
            var parentExists = await _context.Comments.AnyAsync(c => c.Id == request.ParentCommentId.Value, ct);
            if (!parentExists) throw new NotFoundException("Comment", request.ParentCommentId.Value);
        }

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(comment.Id);
    }
}
