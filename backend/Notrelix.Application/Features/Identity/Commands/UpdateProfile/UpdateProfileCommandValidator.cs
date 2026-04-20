using FluentValidation;

namespace Notrelix.Application.Features.Identity.Commands.UpdateProfile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        // Frontend validate form profile; backend chỉ kiểm tra nghiệp vụ không thể xác định ở client.
    }
}

