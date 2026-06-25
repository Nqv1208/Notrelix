using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Collaboration.Attachments.DTOs;

namespace Notrelix.Application.Features.Collaboration.Attachments.Commands.CreateBoardItemAttachment;

public record CreateBoardItemAttachmentCommand(Guid BoardItemId, string Filename, string Url, long? SizeBytes, string? ContentType, string? Source) : ICommand<Result<AttachmentDto>>, ITransactionalRequest;

public class CreateBoardItemAttachmentCommandHandler : IRequestHandler<CreateBoardItemAttachmentCommand, Result<AttachmentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBoardItemAttachmentCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AttachmentDto>> Handle(CreateBoardItemAttachmentCommand request, CancellationToken ct)
    {
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return Result<AttachmentDto>.Failure("Attachment URL must be an absolute HTTP(S) URL.");

        var item = await _context.BoardItems.AsNoTracking()
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
        if (item is null) throw new NotFoundException("BoardItem", request.BoardItemId);

        var now = _dateTimeProvider.UtcNow;
        var target = ResourceRef.Create(ResourceType.BoardItem, request.BoardItemId, item.WorkspaceId);
        var metadata = FileMetadata.Create(request.Filename, request.SizeBytes ?? 0, request.ContentType ?? "application/octet-stream", url: request.Url);
        var attachment = Attachment.Create(item.WorkspaceId, target, AttachmentType.Link, metadata, _currentUser.UserId, now);

        _context.Attachments.Add(attachment);

        return Result<AttachmentDto>.Success(new AttachmentDto(
            attachment.Id,
            attachment.Target.ResourceId,
            attachment.Metadata.FileName,
            attachment.Metadata.Url ?? "",
            attachment.Metadata.Size,
            attachment.Metadata.ContentType,
            attachment.Type.ToString(),
            attachment.CreatedBy!.Value,
            null,
            attachment.CreatedAt.DateTime
        ));
    }
}
