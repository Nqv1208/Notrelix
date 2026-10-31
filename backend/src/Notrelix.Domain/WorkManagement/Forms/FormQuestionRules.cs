using Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Domain.WorkManagement.Forms;

/// <summary>What a question may be and which board field may receive its answer.</summary>
public static class FormQuestionRules
{
    /// <summary>File and signature questions wait until uploads are decided (D70).</summary>
    public static void EnsureSupported(FormQuestionType type)
    {
        if (type is FormQuestionType.FileUpload or FormQuestionType.Signature)
            throw new BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Form_QuestionTypeNotSupported,
                "File and signature questions are not available yet.");
    }

    /// <summary>The answer of a question is stored only in a field of a type that can hold it.</summary>
    public static void EnsureFieldCompatible(FormQuestionType type, FieldType field)
    {
        var ok = type switch
        {
            FormQuestionType.ShortText or FormQuestionType.Email or FormQuestionType.Phone
                => field is FieldType.Text or FieldType.LongText or FieldType.Link,
            FormQuestionType.LongText => field is FieldType.Text or FieldType.LongText,
            FormQuestionType.Number => field is FieldType.Number,
            FormQuestionType.Date => field is FieldType.Date,
            FormQuestionType.Select => field is FieldType.Select or FieldType.Status,
            FormQuestionType.MultiSelect => field is FieldType.MultiSelect,
            FormQuestionType.Checkbox => field is FieldType.Checkbox,
            _ => false,
        };
        if (!ok)
            throw new BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Form_QuestionFieldMismatch,
                $"A {type} question cannot fill a {field} field.");
    }
}
