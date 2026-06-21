namespace Notrelix.Infrastructure.Email;

/// <summary>
/// Skeleton email template renderer (v4 §10). Real implementation renders typed
/// templates (workspace invitation, password reset, email verification,
/// notification) to HTML. Domain never sends email; Application never calls a
/// concrete provider. Not yet wired.
/// </summary>
public sealed class EmailTemplateRenderer
{
    // TODO(v4 §10): Render(templateName, model) -> (subject, htmlBody).
}
