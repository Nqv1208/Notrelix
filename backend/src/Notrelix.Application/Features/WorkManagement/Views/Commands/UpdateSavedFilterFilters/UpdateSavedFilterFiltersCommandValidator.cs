namespace Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterFilters;

public class UpdateSavedFilterFiltersCommandValidator : AbstractValidator<UpdateSavedFilterFiltersCommand>
{
    public UpdateSavedFilterFiltersCommandValidator()
    {
        RuleFor(x => x.FilterId).NotEmpty();
        RuleFor(x => x.Rules).NotNull();
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
    }
}
