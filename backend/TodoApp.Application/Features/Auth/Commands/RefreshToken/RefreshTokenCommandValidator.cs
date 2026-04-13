using FluentValidation;

namespace TodoApp.Application.Features.Auth.Commands.RefreshToken;

// Validator cho RefreshTokenCommand
public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        // Frontend kiểm tra input rỗng; backend xác minh token hợp lệ/hết hạn trong handler.
    }
}
