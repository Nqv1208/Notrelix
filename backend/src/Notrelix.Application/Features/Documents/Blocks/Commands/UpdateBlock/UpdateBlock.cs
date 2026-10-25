using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;

namespace Notrelix.Application.Features.Documents.Blocks.Commands.UpdateBlock;

/// <summary>
/// Edits one block. <c>Type</c> changes the block's type in place (the block
/// keeps its id; text carries over between text-like types, any other change
/// starts empty); <c>Properties</c> then replaces the authored content.
/// </summary>
public record UpdateBlockCommand(
    Guid BlockId,
    string? Type,
    string? Properties
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.block"), BlockId);
}

public class UpdateBlockCommandHandler : IRequestHandler<UpdateBlockCommand, Result>
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

    public async Task<Result> Handle(UpdateBlockCommand request, CancellationToken ct)
    {
        var block = await _context.Blocks.FirstOrDefaultAsync(block => block.Id == request.BlockId && block.DeletedAt == null, ct);
        if (block is null) throw new NotFoundException(nameof(Block), request.BlockId);

        var now = _dateTimeProvider.UtcNow;

        if (request.Type is { Length: > 0 } typeName)
        {
            if (!BlockTypeNames.TryParse(typeName, out var type))
                return Result.Failure($"Unknown block type '{typeName}'.");
            block.ChangeType(type, _currentUser.UserId, now);
        }

        if (request.Properties is { Length: > 0 } properties)
            block.UpdateContent(BlockContent.Create(JsonValue.Create(properties)), _currentUser.UserId, now);

        return Result.Success();
    }
}
