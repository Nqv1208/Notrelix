using MediatR;
using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Common.Interfaces;
using TodoApp.Application.Common.Models;
using TodoApp.Domain.Entities;

namespace TodoApp.Application.Features.Auth.Commands.Register;

// Command đăng ký tài khoản
public record RegisterCommand : IRequest<Result<AuthResult>>
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string Name { get; init; }
}

// Handler cho RegisterCommand
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResult>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public RegisterCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtService jwtService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<Result<AuthResult>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Kiểm tra email đã tồn tại chưa
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email.Value.ToLower() == request.Email.ToLower(), cancellationToken);

        if (emailExists)
        {
            return Result<AuthResult>.Failure("Email is already in use");
        }

        // Tạo user mới
        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = User.Create(request.Email, request.Name, passwordHash);
        _context.Users.Add(user);

        // Tạo workspace cá nhân mặc định
        var workspace = Workspace.CreatePersonal($"{request.Name}'s Workspace", user.Id);
        _context.Workspaces.Add(workspace);

        await _context.SaveChangesAsync(cancellationToken);

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Tạo session
        var session = Session.Create(user.Id, refreshToken, DateTime.UtcNow.AddDays(30));
        _context.Sessions.Add(session);
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
