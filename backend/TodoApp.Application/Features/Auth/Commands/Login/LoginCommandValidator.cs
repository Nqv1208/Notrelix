using FluentValidation;

namespace TodoApp.Application.Features.Auth.Commands.Login;

// Validator cho LoginCommand
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        // Frontend xử lý validation input (email/password bắt buộc, format).
        // Backend chỉ xác thực thông tin đăng nhập thực tế với dữ liệu hệ thống.
    }
}
