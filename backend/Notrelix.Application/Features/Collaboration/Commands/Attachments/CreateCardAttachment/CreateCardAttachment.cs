using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Shared.Attachments.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Shared.Commands.Attachments.CreateCardAttachment;

public record CreateCardAttachmentCommand(Guid BoardItemId, string Filename, string Url, long? SizeBytes, string? ContentType, string? Source) : IRequest<Result<AttachmentDto>>;

public class CreateCardAttachmentCommandHandler : IRequestHandler<CreateCardAttachmentCommand, Result<AttachmentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateCardAttachmentCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<AttachmentDto>> Handle(CreateCardAttachmentCommand request, CancellationToken ct)
    {
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return Result<AttachmentDto>.Failure("Attachment URL must be an absolute HTTP(S) URL.");

        var context = await _context.BoardItems.AsNoTracking()
            .Where(card => card.Id == request.BoardItemId && !card.IsDeleted)
            .Join(_context.BoardGroups.AsNoTracking(),
                card => card.GroupId,
                list => list.Id,
                (card, list) => new { card, list })
            .Join(_context.Boards.AsNoTracking(),
                item => item.list.BoardId,
                board => board.Id,
                (item, board) => new { item.card, board.WorkspaceId })
            .FirstOrDefaultAsync(ct);
        if (context is null) throw new NotFoundException("BoardItem", request.BoardItemId);

        var attachment = Attachment.Create(
            context.WorkspaceId,
            ResourceType.BoardItem,
            request.BoardItemId,
            _currentUser.UserId,
            request.Filename,
            request.Url,
            request.SizeBytes,
            request.ContentType
        );

        _context.Attachments.Add(attachment);
        await _context.SaveChangesAsync(ct);

        return Result<AttachmentDto>.Success(new AttachmentDto(
            attachment.Id,
            attachment.ResourceId,
            attachment.Filename,
            attachment.Url,
            attachment.SizeBytes ?? 0,
            attachment.MimeType ?? "application/octet-stream",
            request.Source ?? "link",
            attachment.UploadedBy,
            null,
            attachment.CreatedAt
        ));
    }
}
