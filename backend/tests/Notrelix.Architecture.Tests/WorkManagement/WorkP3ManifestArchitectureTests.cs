
namespace Notrelix.Architecture.Tests.WorkManagement;

/// <summary>
/// WM-V3-INV-001 — executable P3 source manifest for the Work Management
/// transactional core (docs/workstreams/executions/work-management/work-management.plan.md).
///
/// The manifest is closed-world: every MediatR request under
/// Features.WorkManagement is classified, every request that declares
/// <see cref="IExpectedVersionRequest"/> belongs to the P3 versioned-request target
/// set, and every source file that writes an ordering position is a recorded
/// writer of a named ordering scope. Adding a request, a versioned request or an
/// ordering writer therefore requires an explicit classification here.
/// This is an inventory gate; it is never counted as semantic coverage.
/// </summary>
public sealed class WorkP3ManifestArchitectureTests
{
    public enum Disposition
    {
        /// <summary>In P3 scope; no P3 work unit changes it.</summary>
        Keep,
        /// <summary>In P3 scope; changed by the named work unit(s).</summary>
        Harden,
        /// <summary>Duplicate/legacy authority removed by the named work unit.</summary>
        Retire,
    }

    public sealed record Entry(Disposition Disposition, string Units, bool VersionedTarget = false);

    private const string FeatureNamespace = "Notrelix.Application.Features.WorkManagement.";

    /// <summary>Modules that belong to the P3 transactional core.</summary>
    private static readonly HashSet<string> P3Modules = new(StringComparer.Ordinal)
    {
        "Boards", "BoardItems", "BoardFields", "BoardGroups", "Checklists", "BoardSchema", "Labels",
    };

    /// <summary>Modules outside P3 (Views/Forms/Approvals and other P4 surfaces).</summary>
    private static readonly HashSet<string> OutOfScopeModules = new(StringComparer.Ordinal)
    {
        "Approvals", "BoardPreferences", "BoardViews", "Forms", "ItemLinks", "Relations", "Templates", "Views",
    };

    /// <summary>
    /// Requests in P3 modules that are owned by another capability and stay out of P3.
    /// </summary>
    private static readonly HashSet<string> OutOfScopeRequests = new(StringComparer.Ordinal)
    {
        "BoardItems.ListBoardItemLinksQuery",
        "BoardItems.LinkPageToBoardItemCommand",
        "BoardItems.UnlinkPageFromBoardItemCommand",
        // Platform realtime gateway (PF-09): authorizes a Board stream subscription with the
        // ViewBoard decision of GetBoardQuery; owned by the gateway, not by a P3 capability.
        "Boards.AuthorizeBoardRealtimeSubscriptionQuery",
    };

    /// <summary>
    /// P3 request manifest keyed by "Module.RequestName". VersionedTarget marks the
    /// P3 versioned-request manifest (SPEC WM-V3-DEC-007, WMREQ110–112): the
    /// requests that must carry a required ExpectedVersion once WM-V3-CONC-001 lands.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Entry> P3Requests = new Dictionary<string, Entry>(StringComparer.Ordinal)
    {
        // Board
        ["Boards.GetBoardQuery"] = new(Disposition.Harden, "CONC-002"),
        ["Boards.GetFullBoardQuery"] = new(Disposition.Harden, "CONC-002, QUAL-002"),
        ["Boards.GetBoardsQuery"] = new(Disposition.Keep, "-"),
        ["Boards.GetBoardsBySlugQuery"] = new(Disposition.Keep, "-"),
        ["Boards.GetBoardMembersQuery"] = new(Disposition.Keep, "-"),
        ["Boards.GetBoardAccessQuery"] = new(Disposition.Keep, "-"),
        ["Boards.GetBoardSharingQuery"] = new(Disposition.Keep, "CTR-GAP-WM-BOARD-SHARING (sharing read model)"),
        ["Boards.MakeBoardPrivateCommand"] = new(Disposition.Keep, "CTR-GAP-WM-BOARD-SHARING (atomic make-private)"),
        ["Boards.CreateBoardInWorkspaceCommand"] = new(Disposition.Harden, "APP-BOARD-001 (default-schema ordering writer)"),
        ["Boards.UpdateBoardCommand"] = new(Disposition.Harden, "APP-BOARD-001, CONC-001", VersionedTarget: true),
        ["Boards.MoveBoardToSpaceCommand"] = new(Disposition.Keep, "ADR-009 Space placement (OT-4), CONC-001", VersionedTarget: true),
        ["Boards.UpdateBoardVisibilityCommand"] = new(Disposition.Keep, "-"),
        ["Boards.ArchiveBoardCommand"] = new(Disposition.Keep, "-"),
        ["Boards.UnarchiveBoardCommand"] = new(Disposition.Keep, "-"),
        ["Boards.DeleteBoardCommand"] = new(Disposition.Keep, "-"),
        ["Boards.RestoreBoardCommand"] = new(Disposition.Keep, "LIFE-001 (done)"),
        ["Boards.AddBoardMemberCommand"] = new(Disposition.Keep, "GATE-002 (ManageBoard review)"),
        ["Boards.RemoveBoardMemberCommand"] = new(Disposition.Keep, "GATE-002 (ManageBoard review)"),
        ["Boards.UpdateBoardMemberRoleCommand"] = new(Disposition.Keep, "GATE-002 (ManageBoard review)"),

        // BoardItem / FieldValue
        ["BoardItems.GetBoardItemQuery"] = new(Disposition.Harden, "CONC-002"),
        ["BoardItems.GetBoardItemsQuery"] = new(Disposition.Harden, "CONC-002, QUAL-002"),
        ["BoardItems.GetMyBoardItemsQuery"] = new(Disposition.Keep, "-"),
        ["BoardItems.GetMyWorkExtendedQuery"] = new(Disposition.Keep, "-"),
        ["BoardItems.GetHomeFeedQuery"] = new(Disposition.Keep, "-"),
        ["BoardItems.GetWorkspaceInboxQuery"] = new(Disposition.Keep, "-"),
        ["BoardItems.CreateBoardItemCommand"] = new(Disposition.Harden, "APP-ITEM-001"),
        ["BoardItems.MoveBoardItemCommand"] = new(Disposition.Harden, "APP-ITEM-001, CONC-001", VersionedTarget: true),
        ["BoardItems.DuplicateBoardItemCommand"] = new(Disposition.Harden, "APP-ITEM-002"),
        ["BoardItems.UpdateBoardItemCommand"] = new(Disposition.Harden, "APP-ITEM-002, CONC-001", VersionedTarget: true),
        ["BoardItems.UpdateBoardItemFieldValueCommand"] = new(Disposition.Harden, "APP-ITEM-002, CONC-001", VersionedTarget: true),
        ["BoardItems.UpdateBoardItemFieldValuesCommand"] = new(Disposition.Harden, "APP-ITEM-002, CONC-001", VersionedTarget: true),
        ["BoardItems.ClearFieldValueCommand"] = new(Disposition.Harden, "APP-ITEM-002, CONC-001", VersionedTarget: true),
        ["BoardItems.UpdateBoardItemStatusCommand"] = new(Disposition.Keep, "-"),
        ["BoardItems.SetBoardItemDueDateCommand"] = new(Disposition.Keep, "-"),
        ["BoardItems.CompleteBoardItemCommand"] = new(Disposition.Keep, "-"),
        ["BoardItems.ReopenBoardItemCommand"] = new(Disposition.Keep, "-"),
        ["BoardItems.CreateChildItemCommand"] = new(Disposition.Keep, "-"),
        ["BoardItems.MoveChildItemCommand"] = new(Disposition.Keep, "-", VersionedTarget: true),
        ["BoardItems.ListChildItemsQuery"] = new(Disposition.Keep, "-"),
        ["BoardItems.AssignBoardItemMemberCommand"] = new(Disposition.Keep, "-"),
        ["BoardItems.UnassignBoardItemMemberCommand"] = new(Disposition.Keep, "-"),
        ["BoardItems.ArchiveBoardItemCommand"] = new(Disposition.Keep, "-"),
        ["BoardItems.UnarchiveBoardItemCommand"] = new(Disposition.Keep, "-"),
        ["BoardItems.DeleteBoardItemCommand"] = new(Disposition.Keep, "-"),
        ["BoardItems.RestoreBoardItemCommand"] = new(Disposition.Keep, "LIFE-001 (done)"),

        // BoardField / FieldOption (BoardField-owned family is canonical, SPEC WM-V3-DEC-009)
        ["BoardFields.CreateBoardFieldCommand"] = new(Disposition.Harden, "APP-FIELD-001 (done)"),
        ["BoardFields.UpdateBoardFieldCommand"] = new(Disposition.Harden, "APP-FIELD-001, CONC-001", VersionedTarget: true),
        ["BoardFields.MoveBoardFieldCommand"] = new(Disposition.Harden, "APP-FIELD-001 (done), CONC-001", VersionedTarget: true),
        ["BoardFields.DeleteBoardFieldCommand"] = new(Disposition.Keep, "-"),
        ["BoardFields.AddFieldOptionCommand"] = new(Disposition.Harden, "APP-OPTION-001 (done), CONC-001", VersionedTarget: true),
        ["BoardFields.MoveFieldOptionCommand"] = new(Disposition.Harden, "APP-OPTION-001 (done), CONC-001", VersionedTarget: true),
        ["BoardFields.UpdateFieldOptionCommand"] = new(Disposition.Harden, "APP-OPTION-001 (done), CONC-001", VersionedTarget: true),
        ["BoardFields.RemoveFieldOptionCommand"] = new(Disposition.Harden, "APP-OPTION-001 (done), CONC-001", VersionedTarget: true),

        // BoardGroup
        ["BoardGroups.CreateBoardGroupCommand"] = new(Disposition.Harden, "APP-GROUP-001"),
        ["BoardGroups.UpdateBoardGroupCommand"] = new(Disposition.Harden, "CONC-001", VersionedTarget: true),
        ["BoardGroups.MoveBoardGroupCommand"] = new(Disposition.Harden, "APP-GROUP-001 (done), CONC-001", VersionedTarget: true),
        ["BoardGroups.DuplicateBoardGroupCommand"] = new(Disposition.Harden, "APP-GROUP-001"),
        ["BoardGroups.ArchiveBoardGroupCommand"] = new(Disposition.Keep, "-"),
        ["BoardGroups.UnarchiveBoardGroupCommand"] = new(Disposition.Keep, "-"),
        ["BoardGroups.DeleteBoardGroupCommand"] = new(Disposition.Keep, "FIX-001 (done)", VersionedTarget: true),

        // Checklist (Checklist Version protects ChecklistItem mutation, SPEC WM-V3-DEC-007)
        ["Checklists.GetChecklistsQuery"] = new(Disposition.Harden, "APP-CHK-001 (Version, done), CONC-002"),
        ["Checklists.CreateChecklistCommand"] = new(Disposition.Harden, "APP-CHK-001 (done)"),
        ["Checklists.UpdateChecklistCommand"] = new(Disposition.Harden, "APP-CHK-001 (done), CONC-001", VersionedTarget: true),
        ["Checklists.MoveChecklistCommand"] = new(Disposition.Harden, "APP-CHK-001 (done), CONC-001", VersionedTarget: true),
        ["Checklists.DeleteChecklistCommand"] = new(Disposition.Harden, "APP-CHK-001 (aggregate soft delete, done)"),
        ["Checklists.CreateChecklistItemCommand"] = new(Disposition.Harden, "APP-CHK-001 (done), CONC-001", VersionedTarget: true),
        ["Checklists.MoveChecklistItemCommand"] = new(Disposition.Harden, "APP-CHK-001 (done), CONC-001", VersionedTarget: true),
        ["Checklists.UpdateChecklistItemCommand"] = new(Disposition.Harden, "APP-CHK-001 (done), CONC-001", VersionedTarget: true),
        ["Checklists.ToggleChecklistItemCommand"] = new(Disposition.Harden, "APP-CHK-001 (done), CONC-001", VersionedTarget: true),
        ["Checklists.DeleteChecklistItemCommand"] = new(Disposition.Harden, "APP-CHK-001 (done), CONC-001", VersionedTarget: true),

        // Schema read boundary
        ["BoardSchema.GetBoardSchemaQuery"] = new(Disposition.Harden, "APP-BOUNDARY-002, CONC-002"),

        // Labels (Work-owned item metadata, WMREQ023)
        ["Labels.GetLabelsQuery"] = new(Disposition.Keep, "-"),
        ["Labels.CreateLabelCommand"] = new(Disposition.Keep, "-"),
        ["Labels.UpdateLabelCommand"] = new(Disposition.Keep, "-"),
        ["Labels.DeleteLabelCommand"] = new(Disposition.Keep, "-"),
        ["Labels.AddLabelToBoardItemCommand"] = new(Disposition.Keep, "-"),
        ["Labels.RemoveLabelFromBoardItemCommand"] = new(Disposition.Keep, "-"),
    };

    /// <summary>
    /// Ordering scopes (SPEC WMREQ098) and every source file that writes a position
    /// in them, paths relative to backend/src. WM-V3-DATA-002 may install a scope's
    /// unique index only once every writer listed for that scope is compliant
    /// (SPEC WM-V3-DEC-014); the automation path reaches GroupItems through
    /// MoveBoardItemUseCase (WM-V3-ORDER-005).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]> OrderingWriters = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["GroupItems (board_items: group_id, root items)"] =
        [
            "Notrelix.Application/Features/WorkManagement/BoardItems/Commands/CreateBoardItem/CreateBoardItem.cs",
            "Notrelix.Application/Features/WorkManagement/BoardItems/Services/MoveBoardItemUseCase.cs",
            "Notrelix.Application/Features/WorkManagement/BoardItems/Commands/DuplicateBoardItem/DuplicateBoardItem.cs",
            "Notrelix.Application/Features/WorkManagement/BoardGroups/Commands/DuplicateBoardGroup/DuplicateBoardGroup.cs",
            "Notrelix.Application/Features/WorkManagement/BoardGroups/Commands/DeleteBoardGroup/DeleteBoardGroup.cs",
            "Notrelix.Infrastructure/Data/Seed/InitDb.cs",
        ],
        ["ChildItems (board_items: parent_item_id, sub-items)"] =
        [
            "Notrelix.Application/Features/WorkManagement/BoardItems/Commands/CreateChildItem/CreateChildItem.cs",
            "Notrelix.Application/Features/WorkManagement/BoardItems/Commands/MoveChildItem/MoveChildItem.cs",
        ],
        ["BoardGroups (board_groups: board_id)"] =
        [
            "Notrelix.Application/Features/WorkManagement/BoardGroups/Commands/CreateBoardGroup/CreateBoardGroup.cs",
            "Notrelix.Application/Features/WorkManagement/BoardGroups/Commands/MoveBoardGroup/MoveBoardGroup.cs",
            "Notrelix.Application/Features/WorkManagement/BoardGroups/Commands/DuplicateBoardGroup/DuplicateBoardGroup.cs",
            "Notrelix.Application/Features/WorkManagement/Boards/Commands/CreateBoardInWorkspace/CreateBoardInWorkspace.cs",
            "Notrelix.Infrastructure/Data/Seed/InitDb.cs",
        ],
        ["BoardFields (board_fields: board_id)"] =
        [
            "Notrelix.Application/Features/WorkManagement/BoardFields/Commands/CreateBoardField/CreateBoardField.cs",
            "Notrelix.Application/Features/WorkManagement/BoardFields/Commands/MoveBoardField/MoveBoardField.cs",
            "Notrelix.Infrastructure/Data/Seed/InitDb.cs",
        ],
        ["FieldOptions (field_options: field_id)"] =
        [
            "Notrelix.Application/Features/WorkManagement/BoardFields/Commands/AddFieldOption/AddFieldOption.cs",
            "Notrelix.Application/Features/WorkManagement/BoardFields/Commands/MoveFieldOption/MoveFieldOption.cs",
        ],
        ["ItemChecklists (checklists: item_id)"] =
        [
            "Notrelix.Application/Features/WorkManagement/Checklists/Commands/CreateChecklist/CreateChecklist.cs",
            "Notrelix.Application/Features/WorkManagement/Checklists/Commands/MoveChecklist/MoveChecklist.cs",
        ],
        ["ChecklistItems (checklist_items: checklist_id)"] =
        [
            "Notrelix.Application/Features/WorkManagement/Checklists/Commands/CreateChecklistItem/CreateChecklistItem.cs",
            "Notrelix.Application/Features/WorkManagement/Checklists/Commands/MoveChecklistItem/MoveChecklistItem.cs",
        ],
    };

    /// <summary>
    /// Files that match the writer pattern but write a non-P3 ordering scope or only
    /// define the persistence conversion.
    /// </summary>
    private static readonly HashSet<string> NonP3PositionWriters = new(StringComparer.Ordinal)
    {
        "Notrelix.Application/Features/WorkManagement/Forms/Commands/AddFormQuestion/AddFormQuestion.cs",
        "Notrelix.Application/Features/WorkManagement/Forms/Commands/UpdateFormQuestion/UpdateFormQuestion.cs",
        "Notrelix.Infrastructure/Data/Converters/ValueConverters.cs",
        // Development-only seed repair: places department Spaces (Workspaces' own sibling order) exactly
        // as the seed does; it writes no Work ordering position.
        "Notrelix.Infrastructure/Data/Seed/DevelopmentSeedRepair.cs",
        // The canonical ordering mechanism itself (WM-V3-ORDER-002/004): it generates
        // keys for writers and never persists a position on its own.
        "Notrelix.Application/Features/WorkManagement/Ordering/WorkPlacementResolver.cs",
        "Notrelix.Application/Features/WorkManagement/Ordering/WorkOrderingRebalancer.cs",
    };

    private static readonly Regex PositionWriterPattern = new(
        @"FractionalIndex\.(Create|Initial)\(|FractionalIndexGenerator\.|\.UpdatePosition\(|\.MoveToGroup\(|\.ReorderOptions\(|\.AddOption\(",
        RegexOptions.Compiled);

    [Fact]
    public void EveryWorkManagementRequest_IsClassified_AndNoStaleEntryRemains()
    {
        var discovered = DiscoverWorkRequests();

        var unknownModules = discovered
            .Select(r => r.Module)
            .Where(m => !P3Modules.Contains(m) && !OutOfScopeModules.Contains(m))
            .Distinct()
            .ToList();
        unknownModules.Should().BeEmpty(
            "every Work Management module must be classified P3 or out of scope in WM-V3-INV-001");

        var p3Keys = discovered
            .Where(r => P3Modules.Contains(r.Module))
            .Select(r => r.Key)
            .Where(k => !OutOfScopeRequests.Contains(k))
            .ToHashSet(StringComparer.Ordinal);

        p3Keys.Except(P3Requests.Keys).Should().BeEmpty(
            "every P3 request needs a KEEP/HARDEN/RETIRE classification in the WM-V3-INV-001 manifest");
        P3Requests.Keys.Except(p3Keys).Should().BeEmpty(
            "manifest entries must name a request that still exists; update the manifest when a request is removed or renamed");
        OutOfScopeRequests.Except(discovered.Select(r => r.Key)).Should().BeEmpty(
            "out-of-scope request exceptions must name a request that still exists");
    }

    [Fact]
    public void EveryVersionedWorkRequest_BelongsToTheP3VersionedTarget()
    {
        var versioned = DiscoverWorkRequests()
            .Where(r => P3Modules.Contains(r.Module) && typeof(IExpectedVersionRequest).IsAssignableFrom(r.Type))
            .Select(r => r.Key)
            .ToList();

        var target = P3Requests.Where(e => e.Value.VersionedTarget).Select(e => e.Key).ToHashSet(StringComparer.Ordinal);

        versioned.Except(target).Should().BeEmpty(
            "a P3 request that declares IExpectedVersionRequest must be in the P3 versioned-request manifest (WMREQ112)");
        target.Should().OnlyContain(key => P3Requests[key].Disposition != Disposition.Retire,
            "a retired request cannot be a versioned target");
    }

    [Fact]
    public void EveryOrderingPositionWriter_IsRecordedAgainstItsScope()
    {
        var srcRoot = FindSrcRoot();
        var recorded = OrderingWriters.Values.SelectMany(files => files).ToHashSet(StringComparer.Ordinal);

        recorded.Where(path => !File.Exists(Path.Combine(srcRoot, path))).Should().BeEmpty(
            "every recorded ordering writer must still exist");

        var discovered = new[] { "Notrelix.Application/Features/WorkManagement", "Notrelix.Infrastructure" }
            .SelectMany(dir => Directory.EnumerateFiles(Path.Combine(srcRoot, dir), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}")
                && !path.Contains($"{Path.DirectorySeparatorChar}Configurations{Path.DirectorySeparatorChar}"))
            .Where(path => PositionWriterPattern.IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(srcRoot, path).Replace(Path.DirectorySeparatorChar, '/'))
            .ToList();

        discovered.Should().NotBeEmpty("the writer scan must inspect real source (NRX-016)");
        discovered.Where(path => !recorded.Contains(path) && !NonP3PositionWriters.Contains(path)).Should().BeEmpty(
            "a new writer of an ordering position must be recorded against its scope (SPEC WM-V3-DEC-014)");
    }

    private sealed record WorkRequest(Type Type, string Module, string Key);

    /// <summary>
    /// Authorities removed by their owning unit. Re-adding one (or any request in the
    /// legacy FieldOptions module) recreates a second ordering or option authority.
    /// </summary>
    private static readonly string[] RetiredRequestNames =
    [
        "CreateBoardBySlugCommand",
        "ReorderBoardGroupsCommand",
        "ReorderBoardFieldsCommand",
        "ReorderFieldOptionsCommand",
        "CreateFieldOptionCommand",
        "DeleteFieldOptionCommand",
    ];

    [Fact]
    public void RetiredWorkAuthorities_AreAbsent()
    {
        var discovered = DiscoverWorkRequests();

        discovered.Should().NotBeEmpty("the scan must inspect real Work requests (NRX-016)");
        discovered.Where(r => r.Module == "FieldOptions").Should().BeEmpty(
            "the BoardField-owned option family is the only FieldOption authority (WM-V3-APP-OPTION-001)");
        discovered.Select(r => r.Type.Name).Intersect(RetiredRequestNames).Should().BeEmpty(
            "full-list reorder and legacy create/delete authorities were retired (WM-V3-APP-OPTION-001, APP-GROUP-001, APP-FIELD-001, GATE-002)");
    }

    [Fact]
    public void EveryVersionedTarget_RequiresAPositiveExpectedVersion_AndHasExactlyOneTargetMapping()
    {
        var types = DiscoverWorkRequests().ToDictionary(r => r.Key, r => r.Type, StringComparer.Ordinal);
        var targets = P3Requests.Where(e => e.Value.VersionedTarget).Select(e => e.Key).ToList();
        var validators = typeof(IExpectedVersionRequest).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsGenericTypeDefinition: false } && typeof(FluentValidation.IValidator).IsAssignableFrom(t))
            .Select(t => (Validator: t, Validated: t.BaseType is { IsGenericType: true } b ? b.GetGenericArguments()[0] : null))
            .Where(v => v.Validated is not null)
            .ToLookup(v => v.Validated!, v => v.Validator);

        // 22 from SPEC WM-V3-DEC-007 + MoveBoardToSpaceCommand (ADR-009 Space placement) + MoveChildItemCommand.
        targets.Should().HaveCount(24, "the P3 versioned-request manifest (SPEC WM-V3-DEC-007) is closed-world");
        var violations = new List<string>();
        foreach (var key in targets)
        {
            var type = types[key];
            if (!typeof(IExpectedVersionRequest).IsAssignableFrom(type))
            {
                violations.Add($"{key}: does not declare IExpectedVersionRequest");
                continue;
            }

            var property = type.GetProperty(nameof(IExpectedVersionRequest.ExpectedVersion));
            if (property?.PropertyType != typeof(long))
                violations.Add($"{key}: ExpectedVersion must be a required non-nullable long (no nullable→0 sentinel)");
            if (type.GetConstructors().All(c => c.GetParameters().All(p => p.Name != "ExpectedVersion" || p.HasDefaultValue)))
                violations.Add($"{key}: ExpectedVersion must be a required constructor parameter");
            if (!Notrelix.Infrastructure.Data.Concurrency.ExpectedVersionTargetMap.Default.Contains(type))
                violations.Add($"{key}: no ExpectedVersionTargetMap entry");

            var missing = (IExpectedVersionRequest)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
            var rejectsMissingVersion = validators[type].Any(v =>
                ((FluentValidation.IValidator)Activator.CreateInstance(v)!)
                    .Validate(new FluentValidation.ValidationContext<object>(
                        missing,
                        new FluentValidation.Internal.PropertyChain(),
                        new FluentValidation.Internal.MemberNameValidatorSelector([nameof(IExpectedVersionRequest.ExpectedVersion)])))
                    .Errors.Any(e => e.PropertyName == nameof(IExpectedVersionRequest.ExpectedVersion)));
            if (!rejectsMissingVersion)
                violations.Add($"{key}: no validator rejects a missing (0) ExpectedVersion");
        }

        violations.Should().BeEmpty("WM-V3-CONC-001: every manifest request has a required version and one target mapping");
    }

    /// <summary>
    /// WM-V3-APP-BOUNDARY-001: the Application EF exception (EX-BE-APP-EF-001) must not
    /// grow. The Work persistence port exposes exactly this DbSet set and no database,
    /// raw-SQL or SaveChanges surface; new needs go through a use-case port.
    /// </summary>
    private static readonly string[] WorkDbSetBaseline =
    [
        "Boards", "BoardGroups", "BoardFields", "FieldOptions", "BoardViews", "BoardViewPins", "BoardViewUserPreferences",
        "SavedFilters", "BoardItems", "BoardItemValues", "BoardItemMembers", "BoardItemLabels", "BoardItemLinks", "Labels",
        "BoardMembers", "BoardSubscribers", "BoardRelations", "BoardItemConnections", "MirrorValueSnapshots", "ItemDependencies",
        "TimeTrackingEntries", "RelationFieldConfigs", "Checklists", "ChecklistItems", "Forms", "FormQuestions",
        "FormSubmissions", "ApprovalRequests", "ApprovalSteps", "WorkloadAllocations", "BoardTemplates", "ItemTemplates",
    ];

    [Fact]
    public void WorkManagementPersistencePort_DoesNotGrow()
    {
        var port = typeof(Notrelix.Application.Features.WorkManagement.Abstractions.IWorkManagementDbContext);

        port.GetProperties().Select(p => p.Name).Should().BeEquivalentTo(WorkDbSetBaseline,
            "a new DbSet on the Work port widens the governed Application EF exception (WM-V3-APP-BOUNDARY-001)");
        port.GetMethods().Where(m => !m.IsSpecialName).Should().BeEmpty(
            "the port exposes no SaveChanges, raw SQL or database facade; the data session owns them");
    }

    [Fact]
    public void ChecklistItems_AreNeverAddedOrRemovedOutsideTheChecklistAggregate()
    {
        var featureRoot = Path.Combine(FindSrcRoot(), "Notrelix.Application", "Features");
        var files = Directory.GetFiles(featureRoot, "*.cs", SearchOption.AllDirectories);
        var directWrite = new Regex(@"\.ChecklistItems\s*\.\s*(Add|AddRange|Remove|RemoveRange|Attach|Update)\s*\(", RegexOptions.Compiled);

        files.Should().NotBeEmpty("the scan must inspect real Application source (NRX-016)");
        files.Where(f => directWrite.IsMatch(File.ReadAllText(f)))
            .Select(f => Path.GetRelativePath(featureRoot, f))
            .Should().BeEmpty("a ChecklistItem is a child of the Checklist aggregate; mutate it through Checklist (WM-V3-APP-CHK-001)");
    }

    private static List<WorkRequest> DiscoverWorkRequests() =>
        typeof(IExpectedVersionRequest).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                && t.Namespace is not null
                && t.Namespace.StartsWith(FeatureNamespace, StringComparison.Ordinal)
                && typeof(IBaseRequest).IsAssignableFrom(t))
            .Select(t =>
            {
                var module = t.Namespace!.Substring(FeatureNamespace.Length).Split('.')[0];
                return new WorkRequest(t, module, $"{module}.{t.Name}");
            })
            .ToList();

    private static string FindSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "Notrelix.Application")))
        {
            dir = dir.Parent;
        }

        return dir is null
            ? throw new DirectoryNotFoundException("Could not locate backend/src root from test directory")
            : Path.Combine(dir.FullName, "src");
    }
}
