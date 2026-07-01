using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Documents.Blocks.Commands.DeleteBlock;

public record DeleteBlockCommand(Guid BlockId) : ICommand<Result>, ITransactionalRequest;

public class DeleteBlockCommandHandler : IRequestHandler<DeleteBlockCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    public DeleteBlockCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteBlockCommand request, CancellationToken ct)
    {
        var block = await _context.Blocks.FirstOrDefaultAsync(block => block.Id == request.BlockId && !block.IsDeleted, ct);
        if (block is null) throw new NotFoundException(nameof(Block), request.BlockId);

        block.SoftDelete(_currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
