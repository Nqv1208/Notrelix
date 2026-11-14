using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Retention.DTOs;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Retention;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Features.Chat.Retention.Commands.SetRetentionPolicy;

/// <summary>
/// Sets the Workspace's chat retention rule (UC-CHT-H05, CHT-021): keep messages this many days (7 to 3650) and then turn them into
/// tombstones or purge them, or no window for no retention. Only the Workspace Owner can (the manage-workspace action); a stale
/// expectedVersion is 412. A Workspace that never set a rule is at version 1. The change is audited once, with the old and the new rule; a
/// repeat of the stored rule changes nothing and writes no fact. The job applies the rule in the background; nothing is removed here.
/// </summary>
public record SetRetentionPolicyCommand(Guid WorkspaceId, int? RetentionDays, string Mode, long ExpectedVersion)
    : ICommand<Result<RetentionPolicyDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.retention"), WorkspaceId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class SetRetentionPolicyCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock,
    ChatAuditFacts audit) : IRequestHandler<SetRetentionPolicyCommand, Result<RetentionPolicyDto>>
{
    public async Task<Result<RetentionPolicyDto>> Handle(SetRetentionPolicyCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var canChange = string.Equals(caller.Role, "Owner", StringComparison.Ordinal);
        var mode = RetentionModeWire.FromWire(request.Mode)!.Value;
        var now = clock.UtcNow;

        // Tracked on purpose: the data session binds the expected version to this loaded aggregate.
        var policy = await db.ChatRetentionPolicies.FirstOrDefaultAsync(p => p.WorkspaceId == request.WorkspaceId, cancellationToken);
        var isNew = policy is null;
        if (isNew)
        {
            // No row means no retention at version 1: only a client that read that version may set the first rule.
            if (request.ExpectedVersion != 1)
                throw new PreconditionFailedException(
                    "The retention rule was modified by another request. Reload and retry.", "common.precondition-failed");
            policy = RetentionPolicy.CreateDefault(requestContext.RequireAccountId(), request.WorkspaceId, now);
            // Stored even when nothing changes (the default rule), so the data session has one tracked policy to bind the version to.
            db.ChatRetentionPolicies.Add(policy);
        }

        var before = Describe(policy!);
        if (!policy!.Change(request.RetentionDays, mode, userId, now))
            return Result<RetentionPolicyDto>.Success(RetentionPolicyDto.From(policy, canChange));

        await audit.RetentionChangedAsync(request.WorkspaceId, before, Describe(policy), cancellationToken);
        return Result<RetentionPolicyDto>.Success(RetentionPolicyDto.From(policy, canChange));
    }

    private static object Describe(RetentionPolicy policy) => new { retentionDays = policy.RetentionDays, mode = policy.Mode.ToWire() };
}

public class SetRetentionPolicyCommandValidator : AbstractValidator<SetRetentionPolicyCommand>
{
    public SetRetentionPolicyCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.RetentionDays)
            .Must(RetentionPolicy.IsValidWindow)
            .WithMessage($"The retention window is {ChatLimits.RetentionMinDays} to {ChatLimits.RetentionMaxDays} days, or none.");
        RuleFor(x => x.Mode)
            .Must(mode => RetentionModeWire.FromWire(mode) is not null)
            .WithMessage("mode must be tombstone or purge.");
    }
}
