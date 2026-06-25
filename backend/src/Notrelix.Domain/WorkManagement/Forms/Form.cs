using Notrelix.Domain.WorkManagement.Forms.Events;

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
        form.AddDomainEvent(new FormCreatedDomainEvent(workspaceId, form.Id, boardId, form.Name, createdBy, createdAt));
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
        AddDomainEvent(new FormDetailsUpdatedDomainEvent(WorkspaceId, Id, BoardId, Name, SettingsJson, SubmitterPolicyJson, updatedBy, updatedAt));
    }

    public void Publish(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == FormStatus.Closed)
            throw new BusinessRuleException("Cannot publish a closed form.");

        if (_questions.Count == 0)
            throw new BusinessRuleException("Cannot publish a form with no questions.");

        var oldStatus = Status;
        Status = FormStatus.Published;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new FormPublishedDomainEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void Close(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == FormStatus.Closed) return;

        Status = FormStatus.Closed;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new FormClosedDomainEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void EnsureAcceptsSubmissions()
    {
        EnsureNotDeleted();
        if (Status == FormStatus.Draft)
            throw new BusinessRuleException("Cannot submit to a draft form.");
        if (Status == FormStatus.Closed)
            throw new BusinessRuleException("Cannot submit to a closed form.");
    }

    public void AddQuestion(FormQuestion question, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(question);

        if (Status == FormStatus.Closed)
            throw new BusinessRuleException("Cannot add a question to a closed form.");

        if (question.WorkspaceId != WorkspaceId)
            throw new WorkspaceMismatchException(WorkspaceId, question.WorkspaceId);

        if (_questions.Any(q => q.QuestionKey.Equals(question.QuestionKey, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException($"A question with key '{question.QuestionKey}' already exists.");

        _questions.Add(question);
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new FormQuestionAddedDomainEvent(WorkspaceId, Id, question.QuestionKey, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new FormSoftDeletedDomainEvent(WorkspaceId, Id, BoardId, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new FormRestoredDomainEvent(WorkspaceId, Id, BoardId, restoredBy, restoredAt));
    }
}
