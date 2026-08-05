namespace Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterSorts;

public class UpdateSavedFilterSortsCommandValidator : AbstractValidator<UpdateSavedFilterSortsCommand>
{
    public UpdateSavedFilterSortsCommandValidator()
    {
        RuleFor(x => x.FilterId).NotEmpty();
        RuleFor(x => x.SortRules).NotNull();
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
    }
}
