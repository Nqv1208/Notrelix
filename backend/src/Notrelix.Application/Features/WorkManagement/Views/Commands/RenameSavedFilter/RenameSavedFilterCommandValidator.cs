namespace Notrelix.Application.Features.WorkManagement.Views.Commands.RenameSavedFilter;

public class RenameSavedFilterCommandValidator : AbstractValidator<RenameSavedFilterCommand>
{
    public RenameSavedFilterCommandValidator()
    {
        RuleFor(x => x.FilterId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
    }
}
