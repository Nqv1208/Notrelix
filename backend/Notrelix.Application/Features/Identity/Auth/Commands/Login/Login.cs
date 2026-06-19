using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.Identity.Auth.Commands.Login;

public record LoginCommand : ICommand<Result<AuthResult>>, ITransactionalRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResult>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AuthResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value.ToLower() == request.Email.ToLower(), cancellationToken);

        if (user is null)
            return Result<AuthResult>.Failure("Invalid email or password");

        if (user.Status == UserStatus.Inactive)
            return Result<AuthResult>.Failure("Account has been deactivated");

        if (user.Status == UserStatus.Suspended)
            return Result<AuthResult>.Failure("Account has been suspended");

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return Result<AuthResult>.Failure("Invalid email or password");

        var now = _dateTimeProvider.UtcNow;
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var tokenHash = RefreshTokenHash.Create(refreshToken);

        var session = UserSession.Create(user.Id, tokenHash, now.AddDays(30), now);
        _context.Sessions.Add(session);

        user.RecordLogin(now);

        return Result<AuthResult>.Success(new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email.Value,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl
            }
        });
    }
}
