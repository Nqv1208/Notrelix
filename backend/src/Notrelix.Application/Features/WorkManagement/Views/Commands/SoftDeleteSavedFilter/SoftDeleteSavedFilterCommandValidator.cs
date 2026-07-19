namespace Notrelix.Application.Features.WorkManagement.Views.Commands.SoftDeleteSavedFilter;

public class SoftDeleteSavedFilterCommandValidator : AbstractValidator<SoftDeleteSavedFilterCommand>
{
    public SoftDeleteSavedFilterCommandValidator()
    {
        RuleFor(x => x.FilterId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
    }
}
