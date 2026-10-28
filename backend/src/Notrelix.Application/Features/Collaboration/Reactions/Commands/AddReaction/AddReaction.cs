using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Reactions.Commands.AddReaction;

/// <summary>Reacting needs the same permission as commenting on the item the comment belongs to.</summary>
public record AddReactionCommand(Guid BoardItemId, Guid CommentId, string Emoji) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.CreateComment;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create(ReactionCommentLookup.BoardItemKind), BoardItemId);
}

public class AddReactionCommandValidator : AbstractValidator<AddReactionCommand>
{
    public AddReactionCommandValidator()
    {
        RuleFor(x => x.BoardItemId).NotEmpty();
        RuleFor(x => x.CommentId).NotEmpty();
        RuleFor(x => x.Emoji).Must(ReactionPalette.Contains).WithMessage("Emoji is not in the reaction palette.");
    }
}

public class AddReactionCommandHandler : IRequestHandler<AddReactionCommand, Result>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;

    public AddReactionCommandHandler(ICollaborationDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider, IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(AddReactionCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(AddReactionCommand request, CancellationToken ct)
    {
        var workspaceId = _requestContext.RequireWorkspaceId();
        var accountId = _requestContext.RequireAccountId();
        var userId = _requestContext.UserId;

        var comment = await ReactionCommentLookup.RequireOnItemAsync(_context, request.BoardItemId, request.CommentId, ct);
        var target = ResourceRef.Create(ResourceKind.Create(ReactionCommentLookup.CommentKind), comment.Id, workspaceId);
        var emoji = Emoji.Create(request.Emoji);

        // Adding a reaction the person already holds is a semantic no-op.
        var exists = await _context.Reactions.AsNoTracking()
            .AnyAsync(r => r.Target.Kind == target.Kind && r.Target.ResourceId == comment.Id && r.UserId == userId && r.Emoji.Code == emoji.Code, ct);
        if (exists) return Result.Success();

        _context.Reactions.Add(Reaction.Create(accountId, workspaceId, target, userId, emoji, _dateTimeProvider.UtcNow));
        return Result.Success();
    }
}
