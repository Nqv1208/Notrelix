using System.Text.Json;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Collaboration.Activity.DTOs;
using global::Notrelix.Application.Features.Collaboration.Activity.Ports;

namespace Notrelix.Application.Features.Collaboration.Activity.Queries.GetResourceActivity;

public record GetResourceActivityQuery(ResourceKind ResourceKind, Guid ResourceId, int Page = 1, int PageSize = 20)
    : IQuery<Result<ResourceActivityPageDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public const int MaxPageSize = 100;

    public static GetResourceActivityQuery ForBoardItem(Guid boardItemId, int page = 1, int pageSize = 20)
        => new(ResourceKind.Create(BoardItemKind), boardItemId, page, pageSize);

    private const string BoardItemKind = "work-management.board-item";

    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind, ResourceId);
}

public class GetResourceActivityQueryHandler : IRequestHandler<GetResourceActivityQuery, Result<ResourceActivityPageDto>>
{
    private readonly IResourceActivityReader _reader;
    private readonly IActorLookupService _actorLookup;
    private readonly ICurrentRequestContext _requestContext;

    public GetResourceActivityQueryHandler(
        IResourceActivityReader reader, IActorLookupService actorLookup, ICurrentRequestContext requestContext)
    {
        _reader = reader;
        _actorLookup = actorLookup;
        _requestContext = requestContext;
    }

    public async Task<Result<ResourceActivityPageDto>> Handle(GetResourceActivityQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, GetResourceActivityQuery.MaxPageSize);

        var result = await _reader.ListAsync(
            _requestContext.RequireWorkspaceId(), request.ResourceKind.Value, request.ResourceId, page, pageSize, ct);

        var changes = result.Rows.Select(row => (Row: row, Data: ReadData(row.DataJson))).ToList();
        var userIds = changes.Select(c => c.Row.ActorUserId)
            .Concat(changes.Select(c => c.Data.PersonId))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var actors = (await _actorLookup.FindManyAsync(userIds, ct)).ToDictionary(a => a.UserId);

        var data = changes.Select(c =>
        {
            ActorSnapshot? actor = null;
            if (c.Row.ActorUserId is { } actorId) actors.TryGetValue(actorId, out actor);
            ActorSnapshot? person = null;
            if (c.Data.PersonId is { } personId) actors.TryGetValue(personId, out person);

            var change = c.Data with { PersonName = person?.Name };
            return new ActivityLogDto(
                c.Row.Id,
                c.Row.ActorUserId ?? Guid.Empty,
                actor?.Name,
                c.Row.Title,
                request.ResourceKind.Value,
                request.ResourceId,
                c.Row.ResourceTitle,
                c.Row.OccurredAt.UtcDateTime,
                actor?.AvatarUrl,
                c.Row.ActivityType,
                c.Row.ActivityType == ItemActivityTypes.Created ? "created" : null,
                IsEmpty(change) ? null : change);
        }).ToList();

        return Result<ResourceActivityPageDto>.Success(new ResourceActivityPageDto(data, result.Total, page, pageSize));
    }

    private static ActivityChangeDto ReadData(string dataJson)
    {
        try
        {
            using var document = JsonDocument.Parse(dataJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return Empty;
            return new ActivityChangeDto(
                Text(root, "fieldName"), Text(root, "from"), Text(root, "to"),
                Guid.TryParse(Text(root, "personId"), out var personId) ? personId : null,
                null,
                Text(root, "itemKey"));
        }
        catch (JsonException)
        {
            return Empty;
        }
    }

    private static bool IsEmpty(ActivityChangeDto change) =>
        change.FieldName is null && change.From is null && change.To is null
        && change.PersonId is null && change.PersonName is null && change.ItemKey is null;

    private static readonly ActivityChangeDto Empty = new(null, null, null, null, null, null);

    private static string? Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
