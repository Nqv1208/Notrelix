using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;
using Notrelix.Application.Features.WorkManagement.Templates.Commands.ArchiveBoardTemplate;
using Notrelix.Application.Features.WorkManagement.Templates.Commands.CreateBoardFromTemplate;
using Notrelix.Application.Features.WorkManagement.Templates.Commands.CreateBoardTemplate;
using Notrelix.Application.Features.WorkManagement.Templates.Commands.DeleteBoardTemplate;
using Notrelix.Application.Features.WorkManagement.Templates.Commands.PublishBoardTemplate;
using Notrelix.Application.Features.WorkManagement.Templates.Queries.ListBoardTemplates;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Labels;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;
using IdResult = Notrelix.Application.Common.Models.Result<System.Guid>;

namespace Notrelix.Integration.Tests.Workspaces.OrgTree;

/// <summary>
/// Board templates on PostgreSQL through the real pipeline: saving a board copies its schema, groups and
/// labels (items only when asked) into workspace-scoped creation input, creating from it makes a board that
/// shares no row with the source, and managing a template (publish, archive, delete) is decided on the
/// template's own resource kind, resolved to its workspace.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class BoardTemplateIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public BoardTemplateIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Saves the board as a template and publishes it, as a workspace manager would.</summary>
    private static async Task<Guid> SaveAndPublishAsync(ServiceProvider provider, Guid boardId, string name, string? description, bool includeItems)
    {
        var id = await SendForIdAsync(provider, new CreateBoardTemplateCommand(boardId, name, description, includeItems));
        await OrgTreeTestHost.SendIdempotentAsync(provider, new PublishBoardTemplateCommand(id));
        return id;
    }

    private static async Task<Guid> SendForIdAsync(ServiceProvider provider, object request) =>
        ((IdResult)(await OrgTreeTestHost.SendIdempotentAsync(provider, request))!).Data;

    /// <summary>A board made the normal way (system fields, status options, one group per status) plus a label and one item.</summary>
    private async Task<Guid> SeedSourceBoardAsync(OrgStack stack, ServiceProvider owner)
    {
        var boardId = await SendForIdAsync(owner,
            new CreateBoardInWorkspaceCommand(stack.WorkspaceId, "Sprint", "Two weeks", null, null));
        await _host.WithSystemContextAsync(async c =>
        {
            c.Labels.Add(Label.Create(stack.AccountId, stack.WorkspaceId, boardId, "Bug", LabelColor.Create("#D14343"), stack.OwnerId, OrgTreeTestHost.BaseTime));
            var group = await c.BoardGroups.IgnoreQueryFilters().FirstAsync(g => g.BoardId == boardId);
            var status = await c.BoardFields.IgnoreQueryFilters().FirstAsync(f => f.BoardId == boardId && f.Type == FieldType.Status);
            var option = await c.FieldOptions.IgnoreQueryFilters().FirstAsync(o => o.FieldId == status.Id);
            var item = BoardItem.CreateRoot(stack.AccountId, stack.WorkspaceId, boardId, group.Id, "Plan sprint",
                FractionalIndex.Create("a0"), stack.OwnerId, OrgTreeTestHost.BaseTime, itemKey: "SP-1", itemSequence: 1);
            c.BoardItems.Add(item);
            c.BoardItemValues.Add(BoardItemValue.Create(item.Id, status.Id, FieldValue.Create(JsonValue.Create($"\"{option.Id}\"")!)));
        });
        return boardId;
    }

    [Fact]
    public async Task SaveAsTemplate_ThenCreateBoard_CopiesSchemaWithoutItems_AndSharesNoRow()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var source = await SeedSourceBoardAsync(stack, owner);

        var templateId = await SaveAndPublishAsync(owner, source, "Sprint template", "Reusable", false);
        var boardId = await SendForIdAsync(owner, new CreateBoardFromTemplateCommand(templateId, stack.WorkspaceId, "Sprint 2"));

        await _host.ReadAsync(async c =>
        {
            var fields = await c.BoardFields.IgnoreQueryFilters().Where(f => f.BoardId == boardId).ToListAsync();
            var sourceFields = await c.BoardFields.IgnoreQueryFilters().Where(f => f.BoardId == source).ToListAsync();
            fields.Select(f => f.Name).Should().BeEquivalentTo(sourceFields.Select(f => f.Name));
            fields.Select(f => f.Id).Should().NotIntersectWith(sourceFields.Select(f => f.Id), "a new board owns new field identities");
            var status = fields.Single(f => f.Type == FieldType.Status);
            (await c.FieldOptions.IgnoreQueryFilters().CountAsync(o => o.FieldId == status.Id)).Should().Be(4);
            (await c.BoardGroups.IgnoreQueryFilters().CountAsync(g => g.BoardId == boardId)).Should().Be(4);
            (await c.Labels.IgnoreQueryFilters().SingleAsync(l => l.BoardId == boardId)).Name.Should().Be("Bug");
            (await c.BoardItems.IgnoreQueryFilters().CountAsync(i => i.BoardId == boardId)).Should().Be(0, "items are copied only when asked");
            (await c.Boards.IgnoreQueryFilters().SingleAsync(b => b.Id == boardId)).Description.Should().Be("Reusable");
            return 0;
        });
    }

    [Fact]
    public async Task SaveAsTemplate_WithItems_CopiesTheItemWithItsValuesRemappedToTheNewBoard()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var source = await SeedSourceBoardAsync(stack, owner);

        var templateId = await SaveAndPublishAsync(owner, source, "With items", null, true);
        var boardId = await SendForIdAsync(owner, new CreateBoardFromTemplateCommand(templateId, stack.WorkspaceId, "Copy"));

        await _host.ReadAsync(async c =>
        {
            var item = await c.BoardItems.IgnoreQueryFilters().SingleAsync(i => i.BoardId == boardId);
            item.Name.Should().Be("Plan sprint");
            var status = await c.BoardFields.IgnoreQueryFilters().SingleAsync(f => f.BoardId == boardId && f.Type == FieldType.Status);
            var newOptions = await c.FieldOptions.IgnoreQueryFilters().Where(o => o.FieldId == status.Id).Select(o => o.Id).ToListAsync();
            var value = await c.BoardItemValues.IgnoreQueryFilters().SingleAsync(v => v.ItemId == item.Id);
            value.FieldId.Should().Be(status.Id);
            newOptions.Select(id => $"\"{id}\"").Should().Contain(value.Value.Data.Value);
            return 0;
        });
    }

    [Fact]
    public async Task BuiltInStarter_CreatesABoard_AndTheListCarriesBuiltIns()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);

        var listed = await OrgTreeTestHost.QueryAsync(owner, new ListBoardTemplatesQuery(stack.WorkspaceId));
        var project = listed.Single(t => t.BuiltInKey == "project");
        var boardId = await SendForIdAsync(owner, new CreateBoardFromTemplateCommand(project.Id, stack.WorkspaceId, "Roadmap"));

        await _host.ReadAsync(async c =>
        {
            (await c.BoardFields.IgnoreQueryFilters().CountAsync(f => f.BoardId == boardId)).Should().Be(5, "four system fields and Priority");
            (await c.Labels.IgnoreQueryFilters().CountAsync(l => l.BoardId == boardId)).Should().Be(2);
            return 0;
        });
    }

    [Fact]
    public async Task ADraftTemplate_IsNotOffered_UntilPublished()
    {
        var stack = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var source = await SeedSourceBoardAsync(stack, owner);
        var templateId = await SendForIdAsync(owner, new CreateBoardTemplateCommand(source, "Draft", null, false));

        var draft = () => OrgTreeTestHost.SendIdempotentAsync(owner, new CreateBoardFromTemplateCommand(templateId, stack.WorkspaceId, "Early"));
        await draft.Should().ThrowAsync<AppNotFound>();

        await OrgTreeTestHost.SendIdempotentAsync(owner, new PublishBoardTemplateCommand(templateId));
        await SendForIdAsync(owner, new CreateBoardFromTemplateCommand(templateId, stack.WorkspaceId, "On time"));
    }

    [Fact]
    public async Task ManagingATemplate_NeedsWorkspaceAdmin()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        using var adminProvider = _host.CreateProvider(stack.AccountId, admin);
        using var memberProvider = _host.CreateProvider(stack.AccountId, member);
        var source = await SeedSourceBoardAsync(stack, owner);
        var templateId = await SendForIdAsync(owner, new CreateBoardTemplateCommand(source, "Sprint template", null, false));

        var asMember = () => OrgTreeTestHost.SendIdempotentAsync(memberProvider, new ArchiveBoardTemplateCommand(templateId));
        await asMember.Should().ThrowAsync<AppForbidden>("a Member does not manage workspace templates");
        var saveAsMember = () => OrgTreeTestHost.SendIdempotentAsync(memberProvider, new CreateBoardTemplateCommand(source, "Mine", null, false));
        await saveAsMember.Should().ThrowAsync<AppForbidden>("saving needs the right to manage the board");

        await OrgTreeTestHost.SendIdempotentAsync(adminProvider, new ArchiveBoardTemplateCommand(templateId));
        await OrgTreeTestHost.SendIdempotentAsync(adminProvider, new DeleteBoardTemplateCommand(templateId));
        (await _host.ReadAsync(c => c.BoardTemplates.IgnoreQueryFilters().CountAsync(t => t.Id == templateId && t.DeletedAt == null)))
            .Should().Be(0);
    }

    [Fact]
    public async Task ArchivedTemplate_IsNotOffered_AndATemplateOfAnotherWorkspaceIsNotFound()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        using var otherOwner = _host.CreateProvider(other.AccountId, other.OwnerId);
        var source = await SeedSourceBoardAsync(stack, owner);
        var templateId = await SaveAndPublishAsync(owner, source, "Sprint template", null, false);

        var fromOtherWorkspace = () => OrgTreeTestHost.SendIdempotentAsync(otherOwner, new CreateBoardFromTemplateCommand(templateId, other.WorkspaceId, "Leak"));
        await fromOtherWorkspace.Should().ThrowAsync<AppNotFound>();
        var archiveFromOther = () => OrgTreeTestHost.SendIdempotentAsync(otherOwner, new ArchiveBoardTemplateCommand(templateId));
        await archiveFromOther.Should().ThrowAsync<AppNotFound>("the template is invisible outside its workspace");

        await OrgTreeTestHost.SendIdempotentAsync(owner, new ArchiveBoardTemplateCommand(templateId));
        var archived = () => OrgTreeTestHost.SendIdempotentAsync(owner, new CreateBoardFromTemplateCommand(templateId, stack.WorkspaceId, "Late"));
        await archived.Should().ThrowAsync<AppNotFound>("only published templates create boards");

        var republish = () => OrgTreeTestHost.SendIdempotentAsync(owner, new PublishBoardTemplateCommand(templateId));
        await republish.Should().ThrowAsync<Exception>("an archived template cannot be published again");
    }
}
