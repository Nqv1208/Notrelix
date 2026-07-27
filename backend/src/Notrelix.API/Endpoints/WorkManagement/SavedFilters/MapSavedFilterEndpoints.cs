using Notrelix.API.Endpoints.WorkManagement.SavedFilters.Commands;
using Notrelix.API.Endpoints.WorkManagement.SavedFilters.Queries;

namespace Notrelix.API.Endpoints.WorkManagement.SavedFilters;

public static class MapSavedFilterEndpoints
{
    public static IEndpointRouteBuilder MapSavedFilters(this IEndpointRouteBuilder app)
    {
        var boardGroup = app
            .MapGroup("/api/v1/boards/{boardId:guid}/saved-filters")
            .WithTags("WorkManagement.SavedFilters")
            .WithOpenApi();

        boardGroup.MapCreateSavedFilter();
        boardGroup.MapListSavedFilters();

        var group = app
            .MapGroup("/api/v1/saved-filters/{filterId:guid}")
            .WithTags("WorkManagement.SavedFilters")
            .WithOpenApi();

        group.MapRenameSavedFilter();
        group.MapUpdateSavedFilterVisibility();
        group.MapUpdateSavedFilterFilters();
        group.MapUpdateSavedFilterSorts();
        group.MapUpdateSavedFilterGroup();
        group.MapSoftDeleteSavedFilter();
        group.MapRestoreSavedFilter();

        return app;
    }
}
