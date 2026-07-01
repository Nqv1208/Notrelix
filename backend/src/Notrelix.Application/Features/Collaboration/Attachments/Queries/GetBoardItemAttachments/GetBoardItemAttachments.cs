using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Collaboration.Attachments.DTOs;

namespace Notrelix.Application.Features.Collaboration.Attachments.Queries.GetBoardItemAttachments;

public record GetBoardItemAttachmentsQuery(Guid BoardItemId) : IQuery<Result<List<AttachmentDto>>>;

public class GetBoardItemAttachmentsQueryHandler : IRequestHandler<GetBoardItemAttachmentsQuery, Result<List<AttachmentDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetBoardItemAttachmentsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<AttachmentDto>>> Handle(GetBoardItemAttachmentsQuery request, CancellationToken ct)
    {
        var cardExists = await _context.BoardItems.AsNoTracking()
            .AnyAsync(card => card.Id == request.BoardItemId && !card.IsDeleted, ct);
        if (!cardExists) throw new NotFoundException("BoardItem", request.BoardItemId);

        var attachments = await _context.Attachments.AsNoTracking()
            .Where(attachment => attachment.Target.ResourceType == ResourceType.BoardItem && attachment.Target.ResourceId == request.BoardItemId)
            .OrderByDescending(attachment => attachment.CreatedAt)
            .GroupJoin(_context.Users.AsNoTracking(),
                attachment => attachment.CreatedBy,
                user => (Guid?)user.Id,
                (attachment, users) => new { attachment, user = users.FirstOrDefault() })
            .Select(item => new AttachmentDto(
                item.attachment.Id,
                item.attachment.Target.ResourceId,
                item.attachment.Metadata.FileName,
                item.attachment.Metadata.Url ?? "",
                item.attachment.Metadata.Size,
                item.attachment.Metadata.ContentType,
                item.attachment.Type.ToString(),
                item.attachment.CreatedBy!.Value,
                item.user != null ? item.user.Name : null,
                item.attachment.CreatedAt.DateTime
            ))
            .ToListAsync(ct);

        return Result<List<AttachmentDto>>.Success(attachments);
    }
}
