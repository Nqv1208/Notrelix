using MediatR;
using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Common.Interfaces;
using TodoApp.Application.Common.Models;
using TodoApp.Domain.Entities;

namespace TodoApp.Application.Features.Auth.Commands.UpdateProfile;

public record UpdateProfileCommand : IRequest<Result<UserDto>>
{
    // Filled from JWT claims by controller
    public Guid UserId { get; init; }

    public required string Name { get; init; }
    public string? Avatar { get; init; }
}

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<UserDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateProfileCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UserDto>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result<UserDto>.Failure("Không tìm thấy người dùng");
        }

        user.UpdateProfile(request.Name, request.Avatar);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<UserDto>.Success(new UserDto
        {
            Id = user.Id,
            Email = user.Email.Value,
            Name = user.Name,
            AvatarUrl = user.AvatarUrl
        });
    }
}

