using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.DTOs;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Features.Chat.Channels.Commands.UpdateChannel;

/// <summary>
/// Renames a channel, sets its topic and, when given, its slow mode in seconds (0 turns it off, UC-CHT-H08). Needs a channel manager, a
/// Workspace Owner or Admin (UC-CHT-A03). A slow mode left out stays as it is.
/// </summary>
public record UpdateChannelCommand(Guid WorkspaceId, Guid ChannelId, string Name, string? Topic, long ExpectedVersion, int? SlowModeSeconds = null)
    : ICommand<Result<ChannelDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatChannelManage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.channel"), ChannelId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class UpdateChannelCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock,
    ChatSystemMessages systemMessages) : IRequestHandler<UpdateChannelCommand, Result<ChannelDto>>
{
    public async Task<Result<ChannelDto>> Handle(UpdateChannelCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var space = await ChannelAccess.LoadChannelAsync(db, request.WorkspaceId, request.ChannelId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);
        ChannelAccess.RequireManager(space, caller, userId);

        var name = request.Name.Trim();
        await ChannelAccess.RequireUniqueNameAsync(db, request.WorkspaceId, name, space.Id, cancellationToken);
        var previousName = space.Name;
        space.UpdateDetails(name, request.Topic, userId, clock.UtcNow);
        if (space.Name != previousName)
            await systemMessages.AnnounceAsync(space, userId, ChatSystemMessages.Renamed(space.Name), cancellationToken);
        if (request.SlowModeSeconds is { } slowMode)
            space.SetSlowMode(slowMode, userId, clock.UtcNow);

        return Result<ChannelDto>.Success(ChannelDto.From(space, space.GetActiveMembership(userId), caller.Role));
    }
}

public class UpdateChannelCommandValidator : AbstractValidator<UpdateChannelCommand>
{
    public UpdateChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ChatLimits.ChannelNameMaxLength);
        RuleFor(x => x.Topic).MaximumLength(ChatLimits.ChannelTopicMaxLength).When(x => x.Topic is not null);
        RuleFor(x => x.SlowModeSeconds)
            .InclusiveBetween(0, ChatLimits.SlowModeMaxSeconds)
            .When(x => x.SlowModeSeconds is not null)
            .WithMessage($"slowModeSeconds is 0 to {ChatLimits.SlowModeMaxSeconds}.");
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
