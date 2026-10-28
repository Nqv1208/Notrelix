using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Domain.Documents.Templates;

namespace Notrelix.Application.Features.Documents.Templates.Common;

internal static class PageTemplateMapper
{
    public static PageTemplateDto ToDto(PageTemplate template)
    {
        var (title, icon) = Commands.CreatePageFromTemplate.CreatePageFromTemplateCommandHandler.ReadPageSnapshot(template.PageSnapshot.Value);
        using var blocks = System.Text.Json.JsonDocument.Parse(template.BlocksSnapshot.Value);
        var count = blocks.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array ? blocks.RootElement.GetArrayLength() : 0;
        return new PageTemplateDto(template.Id, template.Name, template.Description, title, icon, count, template.CreatedBy, template.CreatedAt.UtcDateTime);
    }
}
