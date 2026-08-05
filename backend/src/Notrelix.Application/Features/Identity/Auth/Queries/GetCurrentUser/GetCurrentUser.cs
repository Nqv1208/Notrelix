using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;

namespace Notrelix.Application.Features.Identity.Auth.Queries.GetCurrentUser;

// Query lấy thông tin user hiện tại
public record GetCurrentUserQuery : IQuery<Result<UserDto>>, IGlobalRequest, IAuthenticatedRequest
{
    public required Guid UserId { get; init; }
}

// Handler cho GetCurrentUserQuery
public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    private readonly IIdentityDbContext _context;

    public GetCurrentUserQueryHandler(IIdentityDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result<UserDto>.Failure("User not found");
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
}
