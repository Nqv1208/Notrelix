using System.Text.Json;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Services;
using Notrelix.Application.Features.Documents.Common;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Domain.Documents.Rules;

namespace Notrelix.Application.Features.Documents.Blocks.Commands.CreateBlock;

/// <summary>
/// Creates one block. <c>Id</c> lets the editor choose the block's id (so a retry of the same create is
/// recognisable); <c>Content</c> and <c>Properties</c> default to an empty block of the type. A create that
/// repeats an id with the same type, content and properties answers the stored block and changes nothing; the same id
/// with anything else is a conflict.
/// </summary>
public record CreateBlockCommand(
    Guid PageId,
    Guid? Id,
    string Type,
    JsonElement? Content,
    JsonElement? Properties,
    Guid? AfterBlockId,
    Guid? ParentBlockId
) : ICommand<Result<BlockDto>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.UpdatePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class CreateBlockCommandHandler : IRequestHandler<CreateBlockCommand, Result<BlockDto>>
{
    private readonly IDocumentDbContext _context;
    private readonly IPageBlockWriteLock _writeLock;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBlockCommandHandler(IDocumentDbContext context, IPageBlockWriteLock writeLock, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _writeLock = writeLock;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<BlockDto>> Handle(CreateBlockCommand request, CancellationToken ct)
    {
        await _writeLock.AcquireAsync(request.PageId, ct);
        var page = await _context.Pages
            .FirstOrDefaultAsync(p => p.Id == request.PageId && p.DeletedAt == null, ct);
        if (page is null) throw new NotFoundException(nameof(Page), request.PageId);
        PageRules.EnsureCanEdit(page.Status);

        var type = PageBlockSet.ParseType(request.Type);
        var content = PageBlockSet.ContentOf(request.Content) ?? BlockContent.Create(JsonValue.EmptyObject());
        var properties = PageBlockSet.PropertiesOf(request.Properties);
        var accountId = _requestContext.RequireAccountId();

        if (request.Id is { } id && await _context.Blocks.FirstOrDefaultAsync(b => b.Id == id, ct) is { } taken)
        {
            if (taken.PageId == request.PageId && taken.IsRetryOfCreate(type, content, properties))
                return Result<BlockDto>.Success(DocumentDtoMapper.ToBlockDto(taken));
            throw new ConflictException($"Block '{id}' already exists.", "documents.block.id-taken");
        }

        var blocks = await _context.Blocks
            .Where(b => b.PageId == request.PageId && b.DeletedAt == null)
            .ToListAsync(ct);
        var set = new PageBlockSet(accountId, page.WorkspaceId, request.PageId, blocks);
        PageRules.EnsureBlockCountWithinLimit(blocks.Count + 1);

        if (request.ParentBlockId is { } parentId && set.Find(parentId) is null)
            throw new NotFoundException(nameof(Block), parentId);

        var position = set.PositionFor(request.ParentBlockId, request.AfterBlockId, null, null);
        var now = _dateTimeProvider.UtcNow;
        var block = request.ParentBlockId is { } parent
            ? Block.CreateChild(accountId, page.WorkspaceId, request.PageId, type, content, position, _requestContext.UserId, now, set.PathTo(parent), properties, request.Id)
            : Block.CreateRoot(accountId, page.WorkspaceId, request.PageId, type, content, position, _requestContext.UserId, now, properties, request.Id);

        _context.Blocks.Add(block);
        new BlockSaveTracker().Complete(page, [block], _requestContext.UserId, now);
        return Result<BlockDto>.Success(DocumentDtoMapper.ToBlockDto(block));
    }
}
