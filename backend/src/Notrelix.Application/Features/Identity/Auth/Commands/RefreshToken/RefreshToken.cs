using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.Identity.Auth.Commands.RefreshToken;

public record RefreshTokenCommand : ICommand<Result<AuthResult>>, ITransactionalRequest
{
    public required string RefreshToken { get; init; }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResult>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IJwtService jwtService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _jwtService = jwtService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AuthResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var tokenHash = RefreshTokenHash.Create(request.RefreshToken);

        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => 
                s.RefreshTokenHash.Hash == tokenHash.Hash && 
                s.Status == SessionStatus.Active && 
                s.ExpiresAt > now, 
                cancellationToken);

        if (session is null)
        {
            return Result<AuthResult>.Failure("Refresh token is invalid or expired");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == session.UserId, cancellationToken);

        if (user is null)
        {
            return Result<AuthResult>.Failure("User not found");
        }

        session.Revoke(now);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var newTokenHash = RefreshTokenHash.Create(newRefreshToken);

        var newSession = UserSession.Create(user.Id, newTokenHash, now.AddDays(30), now);
        _context.Sessions.Add(newSession);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<AuthResult>.Success(new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
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
