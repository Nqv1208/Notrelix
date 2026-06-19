using Notrelix.API.Endpoints.Automation.Rules.Commands;
using Notrelix.API.Endpoints.Automation.Rules.Queries;

namespace Notrelix.API.Endpoints.Automation.Rules;

public static class MapRuleEndpoints
{
    public static IEndpointRouteBuilder MapRulesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/automations")
            .WithTags("Automation.Rules")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapListAutomationRules();
        group.MapCreateAutomationRule();
        group.MapSetAutomationRuleEnabled();

        return app;
    }
}
