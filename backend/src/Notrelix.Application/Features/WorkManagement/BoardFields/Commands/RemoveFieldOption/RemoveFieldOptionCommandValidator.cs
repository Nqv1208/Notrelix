namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.RemoveFieldOption;

public class RemoveFieldOptionCommandValidator : AbstractValidator<RemoveFieldOptionCommand>
{
    public RemoveFieldOptionCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.FieldId).NotEmpty();
        RuleFor(x => x.OptionId).NotEmpty();
    }
}
