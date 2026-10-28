namespace Notrelix.Application.Features.WorkManagement.Views.Commands.CreateSavedFilter;

public class CreateSavedFilterCommandValidator : AbstractValidator<CreateSavedFilterCommand>
{
    public CreateSavedFilterCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Criteria).MaximumLength(4000)
            .Must(BeAJsonObject).When(x => x.Criteria is not null).WithMessage("Criteria must be a JSON object.");
    }

    private static bool BeAJsonObject(string? criteria)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(criteria!);
            return document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
