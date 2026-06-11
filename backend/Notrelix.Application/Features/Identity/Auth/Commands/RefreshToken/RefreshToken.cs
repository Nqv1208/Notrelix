using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.Identity;

namespace Notrelix.Application.Features.Identity.Auth.Commands.RefreshToken;

// Command làm mới access token
public record RefreshTokenCommand : IRequest<Result<AuthResult>>
{
    public required string RefreshToken { get; init; }
}

// Handler cho RefreshTokenCommand
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResult>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<Result<AuthResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Tìm session hợp lệ theo refresh token
        var session = await _context.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => 
                s.RefreshToken == request.RefreshToken && 
                !s.IsRevoked && 
                s.ExpiresAt > DateTime.UtcNow, 
                cancellationToken);

        if (session is null)
        {
            return Result<AuthResult>.Failure("Refresh token is invalid or expired");
        }

        var user = session.User;

        // Thu hồi session cũ
        session.Revoke();

        // Generate tokens mới
        var accessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        // Tạo session mới
        var newSession = Session.Create(user.Id, newRefreshToken, DateTime.UtcNow.AddDays(30));
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
