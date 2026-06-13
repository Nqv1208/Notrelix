using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.WorkManagement.Boards;

namespace Notrelix.Domain.WorkManagement.Forms;

public class Form : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public FormStatus Status { get; private set; } = FormStatus.Draft;
    public BoardVisibility Visibility { get; private set; } = BoardVisibility.PublicLink;
    public string SettingsJson { get; private set; } = "{}";
    public string SubmitterPolicyJson { get; private set; } = "{}";

    private readonly List<FormQuestion> _questions = new();
    public IReadOnlyCollection<FormQuestion> Questions => _questions.AsReadOnly();

    private Form() : base() { }

    public static Form Create(
        Guid workspaceId,
        Guid boardId,
        string name,
        string slug,
        Guid createdBy,
        DateTimeOffset createdAt,
        BoardVisibility visibility = BoardVisibility.PublicLink,
        string? settingsJson = null,
        string? submitterPolicyJson = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNullOrWhiteSpace(slug);

        var form = new Form
        {
            WorkspaceId = workspaceId,
            BoardId = boardId,
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Status = FormStatus.Draft,
            Visibility = visibility,
            SettingsJson = settingsJson ?? "{}",
            SubmitterPolicyJson = submitterPolicyJson ?? "{}"
        };

        form.SetAuditOnCreate(createdBy, createdAt);
        return form;
    }

    public void UpdateDetails(string name, BoardVisibility visibility, string settingsJson, string submitterPolicyJson, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);

        Name = name.Trim();
        Visibility = visibility;
        SettingsJson = settingsJson ?? "{}";
        SubmitterPolicyJson = submitterPolicyJson ?? "{}";
        
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void Publish(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Status = FormStatus.Published;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void Close(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Status = FormStatus.Closed;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void AddQuestion(FormQuestion question)
    {
        EnsureNotDeleted();
        Guard.NotNull(question);

        if (question.WorkspaceId != WorkspaceId)
            throw new WorkspaceMismatchException(WorkspaceId, question.WorkspaceId);

        if (_questions.Any(q => q.QuestionKey.Equals(question.QuestionKey, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException($"A question with key '{question.QuestionKey}' already exists.");

        _questions.Add(question);
        IncrementVersion();
    }
}
