using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.SetFormPublicAccess;

namespace Notrelix.Application.Features.WorkManagement.Forms.Queries.GetFormPublicAccess;

/// <summary>
/// Whether a form is public and its key, for the people who may change it (Owner and Admin). The key is the
/// public link, so a reader who cannot switch the form is not shown it.
/// </summary>
public record GetFormPublicAccessQuery(Guid FormId)
    : IQuery<Result<FormPublicAccessDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspaceSettings;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.form"), FormId);
}

public class GetFormPublicAccessQueryHandler : IRequestHandler<GetFormPublicAccessQuery, Result<FormPublicAccessDto>>
{
    private readonly IWorkManagementDbContext _context;

    public GetFormPublicAccessQueryHandler(IWorkManagementDbContext context) => _context = context;

    public async Task<Result<FormPublicAccessDto>> Handle(GetFormPublicAccessQuery request, CancellationToken ct)
    {
        var form = await _context.Forms.AsNoTracking().FirstOrDefaultAsync(f => f.Id == request.FormId && f.DeletedAt == null, ct)
            ?? throw new NotFoundException("Form", request.FormId);
        return Result<FormPublicAccessDto>.Success(new FormPublicAccessDto(form.IsPublic, form.PublicKey));
    }
}

public class GetFormPublicAccessQueryValidator : AbstractValidator<GetFormPublicAccessQuery>
{
    public GetFormPublicAccessQueryValidator() => RuleFor(x => x.FormId).NotEmpty();
}
