namespace Notrelix.Domain.WorkManagement.Forms;

/// <summary>A choice of an unmapped Select/MultiSelect question: a stable key (what an answer stores) and its label.</summary>
public sealed class FormQuestionOption : ValueObject
{
    public string Key { get; private set; } = null!;
    public string Label { get; private set; } = null!;

    private FormQuestionOption() { }

    public FormQuestionOption(string key, string label)
    {
        Key = key;
        Label = label;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Key;
        yield return Label;
    }
}
