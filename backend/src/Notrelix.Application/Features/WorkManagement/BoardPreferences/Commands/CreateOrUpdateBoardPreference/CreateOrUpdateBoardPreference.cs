using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.Authorization;

namespace Notrelix.Application.Features.WorkManagement.BoardPreferences.Commands.CreateOrUpdateBoardPreference;

public record CreateOrUpdateBoardPreferenceCommand(
    Guid BoardId,
    Guid ViewId,
    string? Criteria = null,
    string? Columns = null)
    : ICommand<Result>, IWriteRequest, IPersonalBoardRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class CreateOrUpdateBoardPreferenceCommandHandler : IRequestHandler<CreateOrUpdateBoardPreferenceCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateOrUpdateBoardPreferenceCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(CreateOrUpdateBoardPreferenceCommand request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var now = _dateTimeProvider.UtcNow;

        var existing = await _context.BoardViewUserPreferences
            .FirstOrDefaultAsync(p => p.BoardId == request.BoardId && p.ViewId == request.ViewId && p.UserId == userId, ct);

        if (existing is not null)
        {
            if (request.Criteria is not null)
                existing.ApplyCriteria(request.Criteria, now);

            if (request.Columns is not null)
                existing.ApplyColumns(request.Columns, now);

            return Result.Success();
        }

        var preference = BoardViewUserPreference.Create(
            _requestContext.RequireAccountId(),
            _requestContext.RequireWorkspaceId(),
            request.BoardId,
            request.ViewId,
            userId,
            now);

        if (request.Criteria is not null)
            preference.ApplyCriteria(request.Criteria, now);

        if (request.Columns is not null)
            preference.ApplyColumns(request.Columns, now);

        _context.BoardViewUserPreferences.Add(preference);

        return Result.Success();
    }
}

public class CreateOrUpdateBoardPreferenceCommandValidator : AbstractValidator<CreateOrUpdateBoardPreferenceCommand>
{
    public CreateOrUpdateBoardPreferenceCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.ViewId).NotEmpty();
        RuleFor(x => x.Criteria)
            .Must(c => System.Text.Encoding.UTF8.GetByteCount(c!) <= BoardViewUserPreference.MaxCriteriaLength)
            .WithMessage("Criteria must be at most 8 KB.")
            .Must(BeAJsonObjectOrBlank).WithMessage("Criteria must be a JSON object.")
            .When(x => x.Criteria is not null);
        RuleFor(x => x.Columns)
            .Must(c => System.Text.Encoding.UTF8.GetByteCount(c!) <= BoardViewUserPreference.MaxColumnsLength)
            .WithMessage("Columns must be at most 4 KB.")
            .Must(BeAJsonObjectOrBlank).WithMessage("Columns must be a JSON object.")
            .When(x => x.Columns is not null);
    }

    private static bool BeAJsonObjectOrBlank(string? criteria)
    {
        if (string.IsNullOrWhiteSpace(criteria)) return true;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(criteria);
            return document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
