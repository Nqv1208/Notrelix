using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Shared.Attachments.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Shared.Queries.Attachments.GetCardAttachments;

public record GetCardAttachmentsQuery(Guid CardId) : IRequest<Result<List<AttachmentDto>>>;

public class GetCardAttachmentsQueryHandler : IRequestHandler<GetCardAttachmentsQuery, Result<List<AttachmentDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetCardAttachmentsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<AttachmentDto>>> Handle(GetCardAttachmentsQuery request, CancellationToken ct)
    {
        var cardExists = await _context.Cards.AsNoTracking()
            .AnyAsync(card => card.Id == request.CardId && !card.IsDeleted, ct);
        if (!cardExists) throw new NotFoundException("Card", request.CardId);

        var attachments = await _context.Attachments.AsNoTracking()
            .Where(attachment => attachment.ResourceType == ResourceType.Card && attachment.ResourceId == request.CardId)
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
