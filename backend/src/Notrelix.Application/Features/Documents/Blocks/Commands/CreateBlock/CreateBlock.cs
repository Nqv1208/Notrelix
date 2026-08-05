using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.Documents.Blocks.Commands.CreateBlock;

public record CreateBlockCommand(
    Guid PageId,
    BlockType Type,
    string Properties,
    string Position,
    Guid? ParentBlockId
) : ICommand<Result<Guid>>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class CreateBlockCommandHandler : IRequestHandler<CreateBlockCommand, Result<Guid>>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBlockCommandHandler(IDocumentDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateBlockCommand request, CancellationToken ct)
    {
        var page = await _context.Pages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PageId && !p.IsDeleted, ct);
        if (page is null) throw new NotFoundException(nameof(Page), request.PageId);

        var content = BlockContent.Create(JsonValue.Create(request.Properties ?? "{}"));
        var position = FractionalIndex.Create(request.Position);
        var accountId = _requestContext.RequireAccountId();

        Block block;
        if (request.ParentBlockId.HasValue)
        {
            var parentBlock = await _context.Blocks.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == request.ParentBlockId.Value && !b.IsDeleted, ct);
            if (parentBlock is null) throw new NotFoundException(nameof(Block), request.ParentBlockId.Value);

            var ancestorIds = await _context.Blocks.AsNoTracking()
                .Where(b => b.PageId == request.PageId && !b.IsDeleted)
                .Select(b => new { b.Id, b.ParentId })
                .ToListAsync(ct);

            var ancestors = new List<Guid>();
            var current = parentBlock.ParentId;
            while (current.HasValue)
            {
                ancestors.Insert(0, current.Value);
                current = ancestorIds.FirstOrDefault(a => a.Id == current.Value)?.ParentId;
            }

            var parentPath = BlockAncestorPath.Create(accountId, page.WorkspaceId, request.PageId, parentBlock.Id, ancestors);
            block = Block.CreateChild(accountId, page.WorkspaceId, request.PageId, request.Type, content, position, _requestContext.UserId, _dateTimeProvider.UtcNow, parentPath);
        }
        else
        {
            block = Block.CreateRoot(accountId, page.WorkspaceId, request.PageId, request.Type, content, position, _requestContext.UserId, _dateTimeProvider.UtcNow);
        }

        _context.Blocks.Add(block);
        return Result<Guid>.Success(block.Id);
    }
}
