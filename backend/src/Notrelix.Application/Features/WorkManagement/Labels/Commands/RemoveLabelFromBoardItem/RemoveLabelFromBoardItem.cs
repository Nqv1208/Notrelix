using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.RemoveLabelFromBoardItem;

public record RemoveLabelFromBoardItemCommand(Guid BoardItemId, Guid LabelId) : ICommand<Result>, ITransactionalRequest;

public class RemoveLabelFromBoardItemCommandHandler : IRequestHandler<RemoveLabelFromBoardItemCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public RemoveLabelFromBoardItemCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(RemoveLabelFromBoardItemCommand request, CancellationToken ct)
    {
        var cl = await _context.BoardItemLabels
            .FirstOrDefaultAsync(l => l.ItemId == request.BoardItemId && l.LabelId == request.LabelId, ct);
        if (cl is not null)
        {
            _context.BoardItemLabels.Remove(cl);
        }
        return Result.Success();
    }
}
