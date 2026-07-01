using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnassignBoardItemMember;

public record UnassignBoardItemMemberCommand(Guid BoardItemId, Guid UserId) : ICommand<Result>, ITransactionalRequest;

public class UnassignBoardItemMemberCommandHandler : IRequestHandler<UnassignBoardItemMemberCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UnassignBoardItemMemberCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UnassignBoardItemMemberCommand request, CancellationToken ct)
    {
        var member = await _context.BoardItemMembers
            .FirstOrDefaultAsync(m => m.ItemId == request.BoardItemId && m.UserId == request.UserId, ct);
        if (member is not null)
        {
            _context.BoardItemMembers.Remove(member);
        }
        return Result.Success();
    }
}
