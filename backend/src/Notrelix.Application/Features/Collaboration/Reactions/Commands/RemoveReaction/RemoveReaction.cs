using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Reactions.Commands.RemoveReaction;

/// <summary>Withdrawing one's own reaction needs the same permission as commenting on the item.</summary>
public record RemoveReactionCommand(Guid BoardItemId, Guid CommentId, string Emoji) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.CreateComment;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create(ReactionCommentLookup.BoardItemKind), BoardItemId);
}

public class RemoveReactionCommandValidator : AbstractValidator<RemoveReactionCommand>
{
    public RemoveReactionCommandValidator()
    {
        RuleFor(x => x.BoardItemId).NotEmpty();
        RuleFor(x => x.CommentId).NotEmpty();
        RuleFor(x => x.Emoji).Must(ReactionPalette.Contains).WithMessage("Emoji is not in the reaction palette.");
    }
}

public class RemoveReactionCommandHandler : IRequestHandler<RemoveReactionCommand, Result>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;

    public RemoveReactionCommandHandler(ICollaborationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider, IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RemoveReactionCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(RemoveReactionCommand request, CancellationToken ct)
    {
        var comment = await ReactionCommentLookup.RequireOnItemAsync(_context, request.BoardItemId, request.CommentId, ct);
        var commentKind = ResourceKind.Create(ReactionCommentLookup.CommentKind);
        var userId = _currentUser.UserId;

        // Removing a reaction the person does not hold removes nothing.
        var held = await _context.Reactions
            .Where(r => r.Target.Kind == commentKind && r.Target.ResourceId == comment.Id && r.UserId == userId && r.Emoji.Code == request.Emoji)
            .ToListAsync(ct);
        foreach (var reaction in held)
        {
            reaction.Remove(_dateTimeProvider.UtcNow);
            _context.Reactions.Remove(reaction);
        }

        return Result.Success();
    }
}
