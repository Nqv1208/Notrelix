using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Exports.DTOs;
using Notrelix.Application.Features.Chat.RateLimits;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Exports;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Features.Chat.Exports.Commands.CreateChatExport;

/// <summary>
/// Asks for an export of one person's chat data: the messages they wrote and the spaces they belong to, optionally inside a date range
/// (UC-CHT-H06, CHT-021). A Workspace Owner or Admin asks (the manage-workspace-settings action). The request is stored as a queued job and
/// a background job builds the file; the answer is the job, to poll. The person must belong to the Workspace (a member who left or was
/// suspended still does), otherwise 404. The file holds only what the requester may read (CHT-004): a private channel or a conversation the
/// requester is not in is left out and counted. The request is audited and counted against the export limit (429 over it). Nothing about the
/// content is recorded here.
/// </summary>
public record CreateChatExportCommand(Guid WorkspaceId, Guid SubjectUserId, DateTimeOffset? RangeFrom, DateTimeOffset? RangeTo)
    : ICommand<Result<ChatExportDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspaceSettings;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class CreateChatExportCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock,
    IChatRateLimiter rateLimits,
    ChatAuditFacts audit) : IRequestHandler<CreateChatExportCommand, Result<ChatExportDto>>
{
    public async Task<Result<ChatExportDto>> Handle(CreateChatExportCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        // The Workspace is the partition of the second limit: an export is not a space action.
        await rateLimits.RequireAsync(ChatRateLimitPolicy.Export, request.WorkspaceId, userId, cancellationToken);

        var subject = await members.FindAsync(request.WorkspaceId, request.SubjectUserId, cancellationToken)
            ?? throw new NotFoundException("WorkspaceMember", request.SubjectUserId);

        var now = clock.UtcNow;
        var job = ChatExportJob.Request(
            requestContext.RequireAccountId(), request.WorkspaceId, userId, subject.UserId, request.RangeFrom, request.RangeTo, now);
        db.ChatExportJobs.Add(job);
        await audit.ExportRequestedAsync(request.WorkspaceId, job.Id, subject.UserId, job.RangeFrom, job.RangeTo, cancellationToken);
        return Result<ChatExportDto>.Success(ChatExportDto.From(job, now));
    }
}

public class CreateChatExportCommandValidator : AbstractValidator<CreateChatExportCommand>
{
    public CreateChatExportCommandValidator()
    {
        RuleFor(x => x.SubjectUserId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.RangeFrom is null || x.RangeTo is null || x.RangeTo > x.RangeFrom)
            .WithName("range")
            .WithMessage("The range must end after it starts.");
    }
}
