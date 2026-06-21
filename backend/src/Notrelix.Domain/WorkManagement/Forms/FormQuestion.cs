using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Forms;

public class FormQuestion : Entity, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid FormId { get; private set; }
    public Guid? BoardFieldId { get; private set; }
    public string QuestionKey { get; private set; } = null!;
    public string Label { get; private set; } = null!;
    public FormQuestionType QuestionType { get; private set; }
    public bool IsRequired { get; private set; }
    public FractionalIndex Position { get; private set; } = null!;
    public FormQuestionConfig? Config { get; private set; }
    public long Version { get; private set; } = 1;

    private FormQuestion() : base() { }

    public static FormQuestion Create(
        Guid workspaceId,
        Guid formId,
        Guid? boardFieldId,
        string questionKey,
        string label,
        FormQuestionType questionType,
        bool isRequired,
        FractionalIndex position,
        FormQuestionConfig? config = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(formId);
        Guard.NotNullOrWhiteSpace(questionKey);
        Guard.NotNullOrWhiteSpace(label);
        Guard.NotNull(position);

        return new FormQuestion
        {
            WorkspaceId = workspaceId,
            FormId = formId,
            BoardFieldId = boardFieldId,
            QuestionKey = questionKey.Trim(),
            Label = label.Trim(),
            QuestionType = questionType,
            IsRequired = isRequired,
            Position = position,
            Config = config
        };
    }

    public void UpdateQuestion(string label, bool isRequired, FractionalIndex position, FormQuestionConfig? config)
    {
        Guard.NotNullOrWhiteSpace(label);
        Guard.NotNull(position);

        Label = label.Trim();
        IsRequired = isRequired;
        Position = position;
        Config = config;
        Version++;
    }
}
