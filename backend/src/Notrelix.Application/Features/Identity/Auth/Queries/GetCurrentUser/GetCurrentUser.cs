using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;

namespace Notrelix.Application.Features.Identity.Auth.Queries.GetCurrentUser;

// Query lấy thông tin user hiện tại
public record GetCurrentUserQuery : IQuery<Result<UserDto>>, IReadRequest, IGlobalRequest, IAuthenticatedRequest
{
}

// Handler cho GetCurrentUserQuery
public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;

    public GetCurrentUserQueryHandler(IIdentityDbContext context, ICurrentRequestContext currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user is null)
        {
            // A valid session of a person who no longer exists ends like any other dead session (401),
            // so the client signs out instead of showing a validation error.
            return Result<UserDto>.Failure(SessionUserMissingError);
        }

        return Result<UserDto>.Success(new UserDto
        {
            Id = user.Id,
            Email = user.Email.Value,
            Name = user.Name,
            AvatarUrl = user.AvatarUrl,
            EmailConfirmed = user.EmailConfirmed
        });
    }

    private static readonly ApplicationError SessionUserMissingError = new(
        "identity.auth.session-invalid",
        "Session is invalid or expired",
        ApplicationErrorType.Authentication);
}
