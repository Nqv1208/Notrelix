namespace Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterVisibility;

public class UpdateSavedFilterVisibilityCommandValidator : AbstractValidator<UpdateSavedFilterVisibilityCommand>
{
    public UpdateSavedFilterVisibilityCommandValidator()
    {
        RuleFor(x => x.FilterId).NotEmpty();
        RuleFor(x => x.Visibility).IsInEnum();
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
    }
}
