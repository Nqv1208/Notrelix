using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Collaboration.Public.Attachments;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemCover;

/// <summary>
/// Sets the cover of an Item to a colour or to an image attachment of the Item, shown as a strip or over the
/// whole card; both null removes it. The same cover again changes nothing.
/// </summary>
[IdempotencyOperation("work-management.board-items.set-board-item-cover.v1")]
public record SetBoardItemCoverCommand(Guid BoardItemId, string? Color, Guid? AttachmentId, string? Style, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class SetBoardItemCoverCommandHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock,
    IBoardItemImageAttachments images) : IRequestHandler<SetBoardItemCoverCommand, Result>
{
    public async Task<Result> Handle(SetBoardItemCoverCommand request, CancellationToken ct)
    {
        var item = await context.BoardItems.FirstOrDefaultAsync(i => i.Id == request.BoardItemId && i.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        if (request.AttachmentId is { } attachmentId && attachmentId != item.CoverAttachmentId
            && !await images.IsStoredImageOfItemAsync(item.Id, attachmentId, ct))
            throw new BusinessRuleException("The cover image must be an image attached to this item.");

        item.SetCover(request.Color, request.AttachmentId, request.Style, requestContext.UserId, clock.UtcNow);
        return Result.Success();
    }
}
