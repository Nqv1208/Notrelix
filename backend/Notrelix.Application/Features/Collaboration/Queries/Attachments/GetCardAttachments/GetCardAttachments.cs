using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Shared.Attachments.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Shared.Queries.Attachments.GetCardAttachments;

public record GetCardAttachmentsQuery(Guid BoardItemId) : IRequest<Result<List<AttachmentDto>>>;

public class GetCardAttachmentsQueryHandler : IRequestHandler<GetCardAttachmentsQuery, Result<List<AttachmentDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetCardAttachmentsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<AttachmentDto>>> Handle(GetCardAttachmentsQuery request, CancellationToken ct)
    {
        var cardExists = await _context.BoardItems.AsNoTracking()
            .AnyAsync(card => card.Id == request.BoardItemId && !card.IsDeleted, ct);
        if (!cardExists) throw new NotFoundException("BoardItem", request.BoardItemId);

        var attachments = await _context.Attachments.AsNoTracking()
            .Where(attachment => attachment.ResourceType == ResourceType.BoardItem && attachment.ResourceId == request.BoardItemId)
            .OrderByDescending(attachment => attachment.CreatedAt)
            .GroupJoin(_context.Users.AsNoTracking(),
                attachment => attachment.UploadedBy,
                user => user.Id,
                (attachment, users) => new { attachment, user = users.FirstOrDefault() })
            .Select(item => new AttachmentDto(
                item.attachment.Id,
                item.attachment.ResourceId,
                item.attachment.Filename,
                item.attachment.Url,
                item.attachment.SizeBytes ?? 0,
                item.attachment.MimeType ?? "application/octet-stream",
                "link",
                item.attachment.UploadedBy,
                item.user != null ? item.user.Name : null,
                item.attachment.CreatedAt
            ))
            .ToListAsync(ct);

        return Result<List<AttachmentDto>>.Success(attachments);
    }
}
