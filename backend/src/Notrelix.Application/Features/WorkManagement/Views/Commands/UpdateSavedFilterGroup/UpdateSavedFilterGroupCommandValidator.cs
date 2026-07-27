namespace Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterGroup;

public class UpdateSavedFilterGroupCommandValidator : AbstractValidator<UpdateSavedFilterGroupCommand>
{
    public UpdateSavedFilterGroupCommandValidator()
    {
        RuleFor(x => x.FilterId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
    }
}
