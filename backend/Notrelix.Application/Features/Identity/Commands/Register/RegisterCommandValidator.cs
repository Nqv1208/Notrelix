using FluentValidation;

namespace Notrelix.Application.Features.Identity.Commands.Register;

// Validator cho RegisterCommand
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        // Frontend chịu trách nhiệm validate format/register form.
        // Backend chỉ xử lý các validation cần dữ liệu hệ thống (ví dụ: email đã tồn tại) trong handler/domain.
    }
}
