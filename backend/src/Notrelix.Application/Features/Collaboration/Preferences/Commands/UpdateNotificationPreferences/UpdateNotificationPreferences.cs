using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Preferences.DTOs;
using Notrelix.Domain.Collaboration.Preferences;

namespace Notrelix.Application.Features.Collaboration.Preferences.Commands.UpdateNotificationPreferences;

/// <summary>
/// Changes the caller's own notification preference matrix in a Workspace (accepted 2026-10-23, owned by
/// Collaboration). Partial: only the named channels of the named types change, all or nothing; a request
/// whose every value already holds is a no-op that keeps the version. Any member of the Workspace may do
/// it (<c>ViewWorkspace</c>); the subject is always the principal.
/// <see cref="ExpectedVersion"/> is the <c>version</c> of the last read (0 when none was saved). The matrix
/// has no row before its first change, which the pipeline's tracked-aggregate version binding cannot
/// express, so the handler compares the version itself and the aggregate's concurrency token closes the
/// race at the database (the same shape as account preferences).
/// </summary>
public record UpdateNotificationPreferencesCommand(
    Guid WorkspaceId,
    long ExpectedVersion,
    IReadOnlyList<NotificationPreferenceRowChange> Rows)
    : ICommand<Result<NotificationPreferencesDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public sealed class UpdateNotificationPreferencesCommandValidator : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public const int MaxRows = 64;

    public UpdateNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.ExpectedVersion)
            .GreaterThanOrEqualTo(0)
            .WithMessage("ExpectedVersion must be the version from the last read (0 when nothing was saved).");
        RuleFor(x => x.Rows)
            .NotNull()
            .Must(rows => rows is { Count: > 0 and <= MaxRows })
            .WithMessage($"Provide between 1 and {MaxRows} rows.");
        RuleForEach(x => x.Rows).ChildRules(row =>
            row.RuleFor(r => r.Type).NotEmpty().MaximumLength(64));
    }
}

public class UpdateNotificationPreferencesCommandHandler : IRequestHandler<UpdateNotificationPreferencesCommand, Result<NotificationPreferencesDto>>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public UpdateNotificationPreferencesCommandHandler(
        ICollaborationDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider clock)
    {
        _context = context;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result<NotificationPreferencesDto>> Handle(UpdateNotificationPreferencesCommand request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var now = _clock.UtcNow;

        var set = await _context.NotificationPreferenceSets
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.WorkspaceId == request.WorkspaceId && s.UserId == userId, ct);

        var currentVersion = set?.Version ?? 0;
        if (currentVersion != request.ExpectedVersion)
            throw StaleVersion(request.ExpectedVersion, currentVersion);

        var changes = request.Rows
            .SelectMany(row => Cells(row))
            .ToList();

        if (set is null)
        {
            set = NotificationPreferenceSet.Create(_requestContext.RequireAccountId(), request.WorkspaceId, userId, changes, now);
            if (set is not null)
                _context.NotificationPreferenceSets.Add(set);
        }
        else
        {
            set.Apply(changes, now);
        }

        return Result<NotificationPreferencesDto>.Success(NotificationPreferencesDto.From(set));
    }

    private static IEnumerable<NotificationPreferenceChange> Cells(NotificationPreferenceRowChange row)
    {
        if (row.InApp is { } inApp)
            yield return new NotificationPreferenceChange(row.Type, NotificationChannels.InApp, inApp);
        if (row.Email is { } email)
            yield return new NotificationPreferenceChange(row.Type, NotificationChannels.Email, email);
    }

    private static PreconditionFailedException StaleVersion(long expected, long current) => new(
        "Notification preferences were changed elsewhere. Reload and retry.",
        "collaboration.notification-preferences.version-conflict",
        expected,
        current);
}
