using Notrelix.Application.Features.WorkManagement.Common;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.AddFieldOption;

public class AddFieldOptionCommandValidator : AbstractValidator<AddFieldOptionCommand>
{
    public AddFieldOptionCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.FieldId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Category).Must(WorkStatusCategoryNames.IsValidOrNull)
            .WithMessage($"Category must be one of: {string.Join(", ", WorkStatusCategoryNames.All)}.");
    }
}
