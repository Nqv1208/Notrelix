using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.RemoveLabelFromBoardItem;

public record RemoveLabelFromBoardItemCommand(Guid BoardItemId, Guid LabelId) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest
{
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardItem, BoardItemId);
}

public class RemoveLabelFromBoardItemCommandHandler : IRequestHandler<RemoveLabelFromBoardItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    public RemoveLabelFromBoardItemCommandHandler(IWorkManagementDbContext context) => _context = context;

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
