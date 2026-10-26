using System.Text.Json;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Services;
using Notrelix.Application.Features.Documents.Common;
using Notrelix.Application.Features.Documents.DTOs;

namespace Notrelix.Application.Features.Documents.Blocks.Commands.UpdateBlock;

/// <summary>
/// Edits one block against the version the editor read (DCT-011). <c>Type</c> changes the block's type in
/// place (the block keeps its id; rich text and the properties the new type has carry over); <c>Content</c>
/// replaces the authored content and <c>Properties</c> the type's settings. A stale <c>ExpectedVersion</c>
/// fails with 412 carrying the current block.
/// </summary>
public record UpdateBlockCommand(
    Guid PageId,
    Guid BlockId,
    string? Type,
    JsonElement? Content,
    JsonElement? Properties,
    long? ExpectedVersion
) : ICommand<Result<BlockDto>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    // A block is edited with the page it belongs to: the page decides who may (Editor level).
    public PermissionAction Action => PermissionAction.UpdatePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class UpdateBlockCommandHandler : IRequestHandler<UpdateBlockCommand, Result<BlockDto>>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateBlockCommandHandler(IDocumentDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<BlockDto>> Handle(UpdateBlockCommand request, CancellationToken ct)
    {
        var block = await _context.Blocks.FirstOrDefaultAsync(block => block.Id == request.BlockId && block.PageId == request.PageId && block.DeletedAt == null, ct);
        if (block is null) throw new NotFoundException(nameof(Block), request.BlockId);

        PageBlockSet.EnsureVersion(block, request.ExpectedVersion);

        BlockType? type = request.Type is { Length: > 0 } typeName ? PageBlockSet.ParseType(typeName) : null;
        block.Edit(
            type,
            PageBlockSet.ContentOf(request.Content),
            PageBlockSet.PropertiesOf(request.Properties),
            _currentUser.UserId,
            _dateTimeProvider.UtcNow);

        return Result<BlockDto>.Success(DocumentDtoMapper.ToBlockDto(block));
    }
}
