using Notrelix.Application.Common.Storage;
using Notrelix.Application.Common.Tenancy;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Transfer;

/// <summary>
/// Asks for a file of a board's items (UC-WM-B31): the current view under its filter and sort, or every live item.
/// A guest is refused. The file is made in the background and lands in the person's inbox when ready; it is theirs
/// alone and expires after 24 hours. A repeated key makes one export.
/// </summary>
[IdempotencyOperation("work-management.board-exports.create.v1")]
public record CreateBoardExportCommand(Guid BoardId, string Format, string Scope, string? Filter, string? Sort, string? Locale)
    : ICommand<ExportJobDto>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class CreateBoardExportCommandValidator : AbstractValidator<CreateBoardExportCommand>
{
    public CreateBoardExportCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Format).Must(f => f is TabularWriter.Csv or TabularWriter.Xlsx).WithMessage("The format is csv or xlsx.");
        RuleFor(x => x.Scope).Must(s => s is BoardExportBuilder.ScopeView or BoardExportBuilder.ScopeAll).WithMessage("The scope is view or all.");
        RuleFor(x => x.Locale).Must(l => l is null or "vi" or "en").WithMessage("The locale is vi or en.");
    }
}

public class CreateBoardExportCommandHandler(
    IWorkManagementDbContext context, IBoardTransferJobs jobs, IWorkspaceAccessResolver access, ITransferProgress progress,
    ICurrentRequestContext me, IDateTimeProvider clock) : IRequestHandler<CreateBoardExportCommand, ExportJobDto>
{
    public async Task<ExportJobDto> Handle(CreateBoardExportCommand request, CancellationToken ct)
    {
        var board = await context.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == request.BoardId, ct)
            ?? throw new NotFoundException(nameof(Board), request.BoardId);
        await ExportJobs.RequireMemberAsync(access, board.WorkspaceId, me.UserId, ct);
        if (request.Scope == BoardExportBuilder.ScopeView)
            BoardItems.Services.BoardItemCriteria.Parse(request.Filter, request.Sort); // refuses a malformed filter now, not in the background

        var now = clock.UtcNow;
        var name = string.Concat(board.Title.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Trim();
        var options = new ExportOptions(me.RequireAccountId(), $"{name} - {now:yyyy-MM-dd}.{request.Format}", request.Scope, request.Filter, request.Sort, request.Locale ?? "vi");
        var job = new TransferJob
        {
            Kind = TransferKind.Export, Id = Guid.CreateVersion7(), WorkspaceId = board.WorkspaceId, BoardId = board.Id, UserId = me.UserId,
            Status = TransferStatus.Queued, Format = request.Format, OptionsJson = TransferJson.Write(options), CreatedAt = now,
        };
        jobs.Add(job);
        return ExportJobs.ToDto(job, progress, clock);
    }
}
