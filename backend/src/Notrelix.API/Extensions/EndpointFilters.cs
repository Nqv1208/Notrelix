namespace Notrelix.API.Extensions;

/// <summary>
/// Reusable endpoint filters for Minimal APIs.
/// </summary>
public static class EndpointFilters
{
    /// <summary>
    /// Adds FluentValidation filter to an endpoint.
    /// Validates the request body of type <typeparamref name="T"/> 
    /// against registered IValidator&lt;T&gt; before the handler runs.
    /// </summary>
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder)
        where T : class
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
            if (validator is null)
                return await next(context);

            var argument = context.Arguments.OfType<T>().FirstOrDefault();
            if (argument is null)
                return await next(context);

            var validationResult = await validator.ValidateAsync(argument);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());

                return Results.ValidationProblem(errors);
            }

            return await next(context);
        });
    }
}
