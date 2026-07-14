using Notrelix.Application.Features.Identity.Abstractions;

namespace Notrelix.Application.Common.Behaviors;

public sealed class VerifiedEmailBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityUserLookupService _identityUserLookup;

    public VerifiedEmailBehavior(
        ICurrentUser currentUser,
        IIdentityUserLookupService identityUserLookup)
    {
        _currentUser = currentUser;
        _identityUserLookup = identityUserLookup;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IRequireVerifiedEmail)
            return await next();

        if (!_currentUser.IsAuthenticated || _currentUser.UserId == Guid.Empty)
            throw new UnauthorizedException("Authentication required.");

        var user = await _identityUserLookup.FindByIdAsync(
            _currentUser.UserId,
            cancellationToken);

        if (user is null)
            throw new UnauthorizedException("Authentication required.");

        if (!user.EmailConfirmed)
            throw new ForbiddenException("Email must be confirmed before using this feature.");

        return await next();
    }
}
