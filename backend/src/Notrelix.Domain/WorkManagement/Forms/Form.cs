using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Forms.Events;

namespace Notrelix.Domain.WorkManagement.Forms;

public class Form : SoftDeletableAggregateRoot, IWorkspaceScoped
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
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FormQuestion_InvalidConfigJson, $"{propertyName} must be valid JSON.");
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
        form.RaiseDomainEvent(new FormCreatedDomainEvent(accountId, workspaceId, form.Id, boardId, form.Name, createdAt));
        return form;
    }

    public void UpdateDetails(string name, BoardVisibility visibility, string settingsJson, string submitterPolicyJson, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(name);

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = name.Trim();
        Visibility = visibility;
        SettingsJson = ValidateJson(settingsJson, nameof(SettingsJson));
        SubmitterPolicyJson = ValidateJson(submitterPolicyJson, nameof(SubmitterPolicyJson));
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new FormDetailsUpdatedDomainEvent(AccountId, WorkspaceId, Id, BoardId, Name, SettingsJson, SubmitterPolicyJson, updatedBy, updatedAt));
    }

    public void Publish(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        if (Status == FormStatus.Closed)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Form_CannotPublishClosed, "Cannot publish a closed form.");

        if (_questions.Count == 0)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Form_CannotPublishNoQuestions, "Cannot publish a form with no questions.");

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = FormStatus.Published;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new FormPublishedDomainEvent(AccountId, WorkspaceId, Id, updatedAt));
    }

    public void Close(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        if (Status == FormStatus.Closed) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = FormStatus.Closed;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new FormClosedDomainEvent(AccountId, WorkspaceId, Id, updatedAt));
    }

    public void EnsureAcceptsSubmissions()
    {
        EnsureNotDeleted();
        if (Status == FormStatus.Draft)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Form_CannotSubmitToDraft, "Cannot submit to a draft form.");
        if (Status == FormStatus.Closed)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Form_CannotSubmitToClosed, "Cannot submit to a closed form.");
    }

    public void AddQuestion(FormQuestion question, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(question);

        if (Status == FormStatus.Closed)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Form_CannotAddQuestionToClosed, "Cannot add a question to a closed form.");

        if (question.WorkspaceId != WorkspaceId)
            throw new BusinessRuleException(CommonRuleCodes.Common_WorkspaceScopeMismatch, $"Workspace scope mismatch. Expected '{WorkspaceId}', got '{question.WorkspaceId}'.");

        if (_questions.Any(q => q.QuestionKey.Equals(question.QuestionKey, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Form_DuplicateQuestionKey, $"A question with key '{question.QuestionKey}' already exists.");

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        _questions.Add(question);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new FormQuestionAddedDomainEvent(AccountId, WorkspaceId, Id, question.QuestionKey, updatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        var pending = PrepareAuditUpdate(deletedBy, deletedAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new FormSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, BoardId, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        var pending = PrepareAuditUpdate(restoredBy, restoredAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new FormRestoredDomainEvent(AccountId, WorkspaceId, Id, BoardId, restoredBy, restoredAt));
    }
}
