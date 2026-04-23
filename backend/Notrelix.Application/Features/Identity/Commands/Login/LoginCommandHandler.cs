using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.Entities.Identity;
using Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Identity.Commands.Login;

// Command đăng nhập
public record LoginCommand : IRequest<Result<AuthResult>>
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

// Handler cho LoginCommand
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResult>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtService jwtService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<Result<AuthResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Tìm user theo email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value.ToLower() == request.Email.ToLower(), cancellationToken);

        if (user is null)
        {
            return Result<AuthResult>.Failure("Invalid email or password");
        }

        // Kiểm tra trạng thái tài khoản
        if (user.Status == UserStatus.Inactive)
        {
            return Result<AuthResult>.Failure("Account has been deactivated");
        }

        if (user.Status == UserStatus.Suspended)
        {
            return Result<AuthResult>.Failure("Account has been suspended");
        }

        // Xác thực mật khẩu
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result<AuthResult>.Failure("Invalid email or password");
        }

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Tạo session mới
        var session = Session.Create(user.Id, refreshToken, DateTime.UtcNow.AddDays(30));
        _context.Sessions.Add(session);

        // Cập nhật last login
        user.RecordLogin();
        
        await _context.SaveChangesAsync(cancellationToken);

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
