using System.Text.Json;

namespace Notrelix.Application.Features.WorkManagement.Templates.Services;

/// <summary>
/// What a board template carries: the field schema (with Status/Select options), groups, labels and views, and
/// optionally root items. Ids inside it are the source board's; creating a board remaps every one to a new id,
/// so the new board shares nothing with the template (WM-005, WM-028).
/// </summary>
public sealed record BoardTemplateStructure(
    int Version,
    List<TemplateField> Fields,
    List<TemplateGroup> Groups,
    List<TemplateLabel> Labels,
    List<TemplateView> Views,
    List<TemplateItem> Items)
{
    public const int CurrentVersion = 1;

    /// <summary>Most items one template may carry; keeps a template a starting point, not a board backup.</summary>
    public const int MaxItems = 500;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static BoardTemplateStructure Empty() => new(CurrentVersion, [], [], [], [], []);

    public JsonValue ToJson() => JsonValue.Create(JsonSerializer.Serialize(this, Json))!;

    /// <summary>A structure written by an older save may lack a section; a missing section reads as empty.</summary>
    public static BoardTemplateStructure FromJson(JsonValue json)
    {
        var s = JsonSerializer.Deserialize<BoardTemplateStructure>(json.Value, Json) ?? Empty();
        return s with { Fields = s.Fields ?? [], Groups = s.Groups ?? [], Labels = s.Labels ?? [], Views = s.Views ?? [], Items = s.Items ?? [] };
    }
}

public sealed record TemplateOption(Guid Id, string Name, string Color, string? Category);

public sealed record TemplateField(Guid Id, string Name, string Type, bool IsSystem, JsonElement Settings, List<TemplateOption> Options);

public sealed record TemplateGroup(Guid Id, string Name, string Color);

public sealed record TemplateLabel(string Name, string Color);

public sealed record TemplateView(string Name, string Type, bool IsDefault, JsonElement Config);

public sealed record TemplateItem(Guid GroupId, string Title, string? Description, Dictionary<Guid, JsonElement> Values, List<Guid>? Assignees = null);

/// <summary>
/// The starters every workspace offers in the New board gallery. They are code, not rows: the same ids for every
/// workspace, names in the creator's language.
/// </summary>
public static class BuiltInBoardTemplates
{
    public sealed record BuiltIn(Guid Id, string Key, string Name, string Description);

    public static readonly Guid BlankId = Guid.Parse("00000000-0000-0000-0000-00000000b101");
    public static readonly Guid ProjectId = Guid.Parse("00000000-0000-0000-0000-00000000b102");
    public static readonly Guid ContentId = Guid.Parse("00000000-0000-0000-0000-00000000b103");

    public static IReadOnlyList<BuiltIn> All(string? locale)
    {
        var en = IsEnglish(locale);
        return
        [
            new(BlankId, "blank", en ? "Blank board" : "Bảng trống", en ? "Title, status, assignee and due date." : "Tiêu đề, trạng thái, người phụ trách và hạn."),
            new(ProjectId, "project", en ? "Project tracker" : "Theo dõi dự án", en ? "Adds priority and bug / feature labels." : "Thêm mức ưu tiên và nhãn lỗi / tính năng."),
            new(ContentId, "content", en ? "Content plan" : "Kế hoạch nội dung", en ? "Idea to published, with a publish date." : "Từ ý tưởng đến đã đăng, có ngày đăng."),
        ];
    }

    public static bool IsBuiltIn(Guid id) => id == BlankId || id == ProjectId || id == ContentId;

    public static BoardTemplateStructure Structure(Guid id, string? locale)
    {
        var en = IsEnglish(locale);
        var statuses = id == ContentId
            ? new (string Name, string Color, WorkStatusCategory Category)[]
            {
                (en ? "Idea" : "Ý tưởng", "#7A8391", WorkStatusCategory.Todo),
                (en ? "Writing" : "Đang viết", "#1E7BE0", WorkStatusCategory.InProgress),
                (en ? "In review" : "Chờ duyệt", "#C18A12", WorkStatusCategory.Review),
                (en ? "Published" : "Đã đăng", "#2E8B57", WorkStatusCategory.Done),
            }
            : DefaultWorkStatuses.For(locale).Select(s => (s.Name, s.Color, s.Category)).ToArray();

        var options = statuses.Select(s => new TemplateOption(Guid.NewGuid(), s.Name, s.Color, s.Category.ToString())).ToList();
        var empty = JsonSerializer.SerializeToElement(new { });
        var fields = new List<TemplateField>
        {
            new(Guid.NewGuid(), "Title", nameof(FieldType.Text), true, empty, []),
            new(Guid.NewGuid(), "Status", nameof(FieldType.Status), true,
                JsonSerializer.SerializeToElement(new { transitions = new { } }), options),
            new(Guid.NewGuid(), "Assignee", nameof(FieldType.Person), true, empty, []),
            new(Guid.NewGuid(), "Due Date", nameof(FieldType.Date), true, empty, []),
        };
        var labels = new List<TemplateLabel>();

        if (id == ProjectId)
        {
            fields.Add(new(Guid.NewGuid(), en ? "Priority" : "Ưu tiên", nameof(FieldType.Select), false, empty,
            [
                new(Guid.NewGuid(), en ? "High" : "Cao", "#D14343", null),
                new(Guid.NewGuid(), en ? "Medium" : "Trung bình", "#C18A12", null),
                new(Guid.NewGuid(), en ? "Low" : "Thấp", "#7A8391", null),
            ]));
            labels.Add(new(en ? "Bug" : "Lỗi", "#D14343"));
            labels.Add(new(en ? "Feature" : "Tính năng", "#1666CC"));
        }

        if (id == ContentId)
            fields.Add(new(Guid.NewGuid(), en ? "Publish date" : "Ngày đăng", nameof(FieldType.Date), false, empty, []));

        var groups = options.Select(o => new TemplateGroup(Guid.NewGuid(), o.Name, o.Color)).ToList();
        return new BoardTemplateStructure(BoardTemplateStructure.CurrentVersion, fields, groups, labels, [], []);
    }

    private static bool IsEnglish(string? locale) =>
        locale is not null && (locale.Equals("en", StringComparison.OrdinalIgnoreCase)
            || locale.StartsWith("en-", StringComparison.OrdinalIgnoreCase)
            || locale.StartsWith("en_", StringComparison.OrdinalIgnoreCase));
}
