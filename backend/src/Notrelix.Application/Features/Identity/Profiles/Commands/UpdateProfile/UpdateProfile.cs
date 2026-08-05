using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;

namespace Notrelix.Application.Features.Identity.Profiles.Commands.UpdateProfile;

public record UpdateProfileCommand : ICommand<Result<UserDto>>, ITransactionalRequest, IGlobalRequest
{
    // Filled from JWT claims by controller
    public Guid UserId { get; init; }

    public required string Name { get; init; }
    public string? Avatar { get; init; }
}

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<UserDto>>
{
    private readonly IIdentityDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateProfileCommandHandler(IIdentityDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<UserDto>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result<UserDto>.Failure("User not found");
        }

        user.UpdateProfile(request.Name, request.Avatar, request.UserId, _dateTimeProvider.UtcNow);

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
