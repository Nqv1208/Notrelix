using System.Text.Json.Serialization;
using FluentValidation.Results;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Transfer;

public record GetBoardImportQuery(Guid BoardId, Guid JobId)
    : IQuery<ImportJobDto>, IReadRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class GetBoardImportQueryHandler(IBoardTransferJobs jobs, ITransferProgress progress, ICurrentRequestContext me)
    : IRequestHandler<GetBoardImportQuery, ImportJobDto>
{
    public async Task<ImportJobDto> Handle(GetBoardImportQuery request, CancellationToken ct) =>
        ImportJobs.ToDto(await ImportJobs.OwnAsync(jobs, me, request.BoardId, request.JobId, ct), progress);
}
