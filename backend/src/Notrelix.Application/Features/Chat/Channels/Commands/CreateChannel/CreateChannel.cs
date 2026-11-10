using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.DTOs;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Channels.Commands.CreateChannel;

/// <summary>
/// Creates a public or private channel; the caller becomes its first manager (UC-CHT-A01, A06). Retrying with the
/// same Idempotency-Key returns the channel the first attempt created.
/// </summary>
[IdempotencyOperation("chat.channels.create-channel.v1")]
public record CreateChannelCommand(Guid WorkspaceId, string Name, string? Topic, bool IsPrivate, Guid? GroupId)
    : ICommand<Result<ChannelDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ChatChannelCreate;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class CreateChannelCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock) : IRequestHandler<CreateChannelCommand, Result<ChannelDto>>
{
    public async Task<Result<ChannelDto>> Handle(CreateChannelCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var accountId = requestContext.RequireAccountId();
        var name = request.Name.Trim();

        await ChannelAccess.RequireUniqueNameAsync(db, request.WorkspaceId, name, null, cancellationToken);
        if (request.GroupId is { } groupId)
            await ChannelAccess.RequireGroupAsync(db, request.WorkspaceId, groupId, cancellationToken);

        var position = await ChannelAccess.NextChannelPositionAsync(db, request.WorkspaceId, cancellationToken);
        var kind = request.IsPrivate ? SpaceKind.ChannelPrivate : SpaceKind.ChannelPublic;
        var space = Space.CreateChannel(
            accountId, request.WorkspaceId, kind, name, request.Topic, request.GroupId, position, userId, clock.UtcNow);

        db.ChatSpaces.Add(space);
        return Result<ChannelDto>.Success(ChannelDto.From(space, space.GetActiveMembership(userId)));
    }
}

public class CreateChannelCommandValidator : AbstractValidator<CreateChannelCommand>
{
    public CreateChannelCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ChatLimits.ChannelNameMaxLength);
        RuleFor(x => x.Topic).MaximumLength(ChatLimits.ChannelTopicMaxLength).When(x => x.Topic is not null);
    }
}
