using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Common;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Documents.Versions;

namespace Notrelix.Application.Features.Documents.Versions.Commands.CreatePageVersion;

/// <summary>Saves the page as it is now as a named history entry. A person who may edit the page may do it.</summary>
public record CreatePageVersionCommand(Guid PageId, string Name) : ICommand<Result<PageVersionDto>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.UpdatePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class CreatePageVersionCommandHandler : IRequestHandler<CreatePageVersionCommand, Result<PageVersionDto>>
{
    private readonly IDocumentDbContext _context;
    private readonly IPageVersionRecorder _recorder;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreatePageVersionCommandHandler(IDocumentDbContext context, IPageVersionRecorder recorder, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _recorder = recorder;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<PageVersionDto>> Handle(CreatePageVersionCommand request, CancellationToken ct)
    {
        var page = await _context.Pages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PageId && p.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(Page), request.PageId);
        if (page.Status == PageStatus.Archived)
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(DocumentRuleCodes.Documents_Page_CannotEditArchived, "An archived page cannot get a new version.");

        var live = await _context.Blocks.AsNoTracking()
            .Where(b => b.PageId == request.PageId && b.DeletedAt == null)
            .ToListAsync(ct);

        var version = await _recorder.AddAsync(page, live, _requestContext.UserId, DocumentVersionKind.Named, request.Name, _dateTimeProvider.UtcNow, ct);
        return Result<PageVersionDto>.Success(DocumentDtoMapper.ToVersionDto(version));
    }
}
