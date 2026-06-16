using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Domain.Documents.Blocks;
using global::Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.Document.Commands.Blocks.CreateBlock;

public record CreateBlockCommand(
    Guid PageId,
    string Type,
    string Properties,
    string Position,
    Guid? ParentBlockId
) : IRequest<Result<Guid>>;

public class CreateBlockCommandHandler : IRequestHandler<CreateBlockCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBlockCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateBlockCommand request, CancellationToken ct)
    {
        var page = await _context.Pages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PageId && !p.IsDeleted, ct);
        if (page is null) throw new NotFoundException(nameof(Page), request.PageId);

        var blockType = Enum.Parse<BlockType>(request.Type, ignoreCase: true);
        var content = BlockContent.Create(JsonValue.Create(request.Properties ?? "{}"));
        var position = FractionalIndex.Create(request.Position);

        var block = Block.Create(page.WorkspaceId, request.PageId, blockType, content, position, _currentUser.UserId, _dateTimeProvider.UtcNow, parentId: request.ParentBlockId);
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(block.Id);
    }
}
