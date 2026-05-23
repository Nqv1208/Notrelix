using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Entities.Shared;
using Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Shared.Comments;

// ── DTOs ─────────────────────────────────────────────────────
public record CommentDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string? UserAvatar,
    string ContentMd,
    Guid? ParentCommentId,
    bool IsEdited,
    DateTime? ResolvedAt,
    DateTime CreatedAt
);

// ── Queries ──────────────────────────────────────────────────
public record GetCommentsQuery(string ResourceType, Guid ResourceId) : IRequest<Result<List<CommentDto>>>;

// ── Commands ─────────────────────────────────────────────────
public record CreateCommentCommand(string ResourceType, Guid ResourceId, string ContentMd, Guid? ParentCommentId) : IRequest<Result<Guid>>;
public record UpdateCommentCommand(Guid CommentId, string ContentMd) : IRequest<Result>;
public record DeleteCommentCommand(Guid CommentId) : IRequest<Result>;
public record ResolveCommentCommand(Guid CommentId) : IRequest<Result>;

// ── Handlers ─────────────────────────────────────────────────

public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, Result<List<CommentDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetCommentsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<CommentDto>>> Handle(GetCommentsQuery request, CancellationToken ct)
    {
        var resourceType = Enum.Parse<ResourceType>(request.ResourceType, ignoreCase: true);

        var comments = await _context.Comments.AsNoTracking()
            .Where(c => c.ResourceType == resourceType && c.ResourceId == request.ResourceId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .Join(_context.Users.AsNoTracking(), c => c.UserId, u => u.Id,
                (c, u) => new CommentDto(
                    c.Id, c.UserId, u.Name, u.AvatarUrl,
                    c.ContentMd, c.ParentCommentId, c.IsEdited,
                    c.ResolvedAt, c.CreatedAt
                ))
            .ToListAsync(ct);

        return Result<List<CommentDto>>.Success(comments);
    }
}

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

public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UpdateCommentCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateCommentCommand request, CancellationToken ct)
    {
        var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == request.CommentId, ct);
        if (comment is null) throw new NotFoundException(nameof(Comment), request.CommentId);
        comment.Edit(request.ContentMd);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public DeleteCommentCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteCommentCommand request, CancellationToken ct)
    {
        var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == request.CommentId, ct);
        if (comment is null) throw new NotFoundException(nameof(Comment), request.CommentId);
        comment.SoftDelete();
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class ResolveCommentCommandHandler : IRequestHandler<ResolveCommentCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public ResolveCommentCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ResolveCommentCommand request, CancellationToken ct)
    {
        var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == request.CommentId, ct);
        if (comment is null) throw new NotFoundException(nameof(Comment), request.CommentId);
        comment.Resolve(_currentUser.UserId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
