using Notrelix.Application.Features.Operators.Abstractions;
using Notrelix.Domain.Operators;

namespace Notrelix.Infrastructure.Operators;

/// <summary>
/// Runs on every operator route after the operator token was verified. It finds the operator row and refuses the
/// request unless the operator is still active, so disabling an operator ends their access at once, not when the
/// token runs out. It then sets the request's current operator, the actor every audit entry names.
/// </summary>
public sealed class OperatorSessionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var subject = http.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(subject, out var operatorId)
            || http.User.FindFirst(OperatorOptions.TokenUseClaim)?.Value != OperatorOptions.TokenUseOperator)
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
        }

        var operators = http.RequestServices.GetRequiredService<IOperatorsDbContext>();
        var found = await operators.OperatorAccounts.AsNoTracking()
            .Where(o => o.Id == operatorId)
            .Select(o => new { o.Email, o.DisplayName, o.Status })
            .FirstOrDefaultAsync(http.RequestAborted);
        if (found is null || found.Status != OperatorStatus.Active)
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
        }

        http.RequestServices.GetRequiredService<CurrentOperator>().Set(operatorId, found.Email, found.DisplayName);
        return await next(context);
    }
}
