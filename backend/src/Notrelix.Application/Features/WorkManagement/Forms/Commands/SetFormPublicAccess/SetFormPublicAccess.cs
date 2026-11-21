using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.Sharing;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.SetFormPublicAccess;

/// <summary>The public state of a form as its managers see it; the key is the whole public link.</summary>
public record FormPublicAccessDto(bool IsPublic, string? PublicKey);

/// <summary>
/// Switches a form between private (the default) and public. Only an Owner or Admin of the form's workspace may
/// (ManageWorkspaceSettings, so a Member or Guest is refused whatever they hold on the board), and the
/// workspace must allow public links. Going private clears the key at once, so every copy of the link stops
/// working; going public again mints a fresh key. Switching to the state the form already has changes nothing.
/// </summary>
public record SetFormPublicAccessCommand(Guid FormId, bool Enabled)
    : ICommand<Result<FormPublicAccessDto>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspaceSettings;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.form"), FormId);
}

public class SetFormPublicAccessCommandHandler : IRequestHandler<SetFormPublicAccessCommand, Result<FormPublicAccessDto>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;
    private readonly IWorkspaceSharingFacts _sharing;

    public SetFormPublicAccessCommandHandler(
        IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider clock, IWorkspaceSharingFacts sharing)
    {
        _context = context;
        _requestContext = requestContext;
        _clock = clock;
        _sharing = sharing;
    }

    public async Task<Result<FormPublicAccessDto>> Handle(SetFormPublicAccessCommand request, CancellationToken ct)
    {
        var form = await _context.Forms.FirstOrDefaultAsync(f => f.Id == request.FormId && f.DeletedAt == null, ct)
            ?? throw new NotFoundException("Form", request.FormId);

        if (request.Enabled)
        {
            var facts = await _sharing.FindAsync(form.WorkspaceId, ct);
            if (facts is not { IsActive: true, AllowsPublicLinks: true })
                throw new DomainBusinessRuleException(
                    WorkManagementRuleCodes.WorkManagement_Form_PublicSharingDisabled,
                    "This workspace does not allow public links. An Owner or Admin can turn them on in the workspace settings.");
            form.MakePublic(Form.NewPublicKey(), _requestContext.UserId, _clock.UtcNow);
        }
        else
        {
            form.MakePrivate(_requestContext.UserId, _clock.UtcNow);
        }

        return Result<FormPublicAccessDto>.Success(new FormPublicAccessDto(form.IsPublic, form.PublicKey));
    }
}

public class SetFormPublicAccessCommandValidator : AbstractValidator<SetFormPublicAccessCommand>
{
    public SetFormPublicAccessCommandValidator() => RuleFor(x => x.FormId).NotEmpty();
}
