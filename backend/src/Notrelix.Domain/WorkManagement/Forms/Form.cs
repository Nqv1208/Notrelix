using Notrelix.Domain.WorkManagement.Forms.Events;

namespace Notrelix.Domain.WorkManagement.Forms;

public class Form : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
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

    private static string ValidateJson(string? value, string propertyName)
    {
        var json = value ?? "{}";
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
        }
        catch (System.Text.Json.JsonException)
        {
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_FormQuestion_InvalidConfigJson, $"{propertyName} must be valid JSON.");
        }
        return json;
    }

    public static Form Create(
        Guid accountId,
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
        Guard.NotEmpty(accountId);

        var form = new Form
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Status = FormStatus.Draft,
            Visibility = visibility,
            SettingsJson = ValidateJson(settingsJson, nameof(SettingsJson)),
            SubmitterPolicyJson = ValidateJson(submitterPolicyJson, nameof(SubmitterPolicyJson))
        };

        form.SetAuditOnCreate(createdBy, createdAt);
        form.RaiseDomainEvent(new FormCreatedDomainEvent(accountId, workspaceId, form.Id, boardId, form.Name, createdBy, createdAt));
        return form;
    }

    public void UpdateDetails(string name, BoardVisibility visibility, string settingsJson, string submitterPolicyJson, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);

        Name = name.Trim();
        Visibility = visibility;
        SettingsJson = ValidateJson(settingsJson, nameof(SettingsJson));
        SubmitterPolicyJson = ValidateJson(submitterPolicyJson, nameof(SubmitterPolicyJson));

        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new FormDetailsUpdatedDomainEvent(AccountId, WorkspaceId, Id, BoardId, Name, SettingsJson, SubmitterPolicyJson, updatedBy, updatedAt));
    }

    public void Publish(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == FormStatus.Closed)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Form_CannotPublishClosed, "Cannot publish a closed form.");

        if (_questions.Count == 0)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Form_CannotPublishNoQuestions, "Cannot publish a form with no questions.");

        var oldStatus = Status;
        Status = FormStatus.Published;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new FormPublishedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void Close(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == FormStatus.Closed) return;

        Status = FormStatus.Closed;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new FormClosedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void EnsureAcceptsSubmissions()
    {
        EnsureNotDeleted();
        if (Status == FormStatus.Draft)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Form_CannotSubmitToDraft, "Cannot submit to a draft form.");
        if (Status == FormStatus.Closed)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Form_CannotSubmitToClosed, "Cannot submit to a closed form.");
    }

    public void AddQuestion(FormQuestion question, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(question);

        if (Status == FormStatus.Closed)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Form_CannotAddQuestionToClosed, "Cannot add a question to a closed form.");

        if (question.WorkspaceId != WorkspaceId)
            throw new WorkspaceMismatchException(WorkspaceId, question.WorkspaceId);

        if (_questions.Any(q => q.QuestionKey.Equals(question.QuestionKey, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_Form_DuplicateQuestionKey, $"A question with key '{question.QuestionKey}' already exists.");

        _questions.Add(question);
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new FormQuestionAddedDomainEvent(AccountId, WorkspaceId, Id, question.QuestionKey, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new FormSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, BoardId, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new FormRestoredDomainEvent(AccountId, WorkspaceId, Id, BoardId, restoredBy, restoredAt));
    }
}
