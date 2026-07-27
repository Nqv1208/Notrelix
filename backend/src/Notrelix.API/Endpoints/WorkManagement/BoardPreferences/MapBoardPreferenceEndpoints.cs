using Notrelix.API.Endpoints.WorkManagement.BoardPreferences.Commands;
using Notrelix.API.Endpoints.WorkManagement.BoardPreferences.Queries;

namespace Notrelix.API.Endpoints.WorkManagement.BoardPreferences;

public static class MapBoardPreferenceEndpoints
{
    public static IEndpointRouteBuilder MapBoardPreferences(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/boards/{boardId:guid}/preferences/{viewId:guid}")
            .WithTags("WorkManagement.BoardPreferences")
            .WithOpenApi();

        group.MapGetBoardPreference();
        group.MapCreateOrUpdateBoardPreference();
        group.MapUpdateBoardPreferenceFilters();
        group.MapUpdateBoardPreferenceSorts();
        group.MapUpdateBoardPreferenceGroup();

        return app;
    }
}
