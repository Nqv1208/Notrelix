using Microsoft.EntityFrameworkCore.Metadata;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Common;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Integration;

/// <summary>
/// Tests the workspace isolation query filter configuration by building the EF Core model
/// and inspecting <see cref="IReadOnlyEntityType.GetQueryFilter"/>.
///
/// EF Core's InMemory provider does NOT enforce query filters at query time,
/// so these tests verify the filter structure at the model configuration layer.
/// Runtime isolation is verified via real PostgreSQL in Docker integration tests.
/// </summary>
public class TenantIsolationTests
{
    private static readonly Guid WorkspaceA = Guid.Parse("A0000000-0000-0000-0000-000000000001");

    [Fact]
    public void AllWorkspaceScopedEntities_HaveWorkspaceQueryFilter()
    {
        var model = BuildModel(new FakeCurrentTenantContext());

        var scopedTypes = typeof(IWorkspaceScoped).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IWorkspaceScoped).IsAssignableFrom(t))
            .ToList();

        scopedTypes.Should().NotBeEmpty("at least one IWorkspaceScoped entity exists");

        foreach (var scopedType in scopedTypes)
        {
            var entityType = model.FindEntityType(scopedType);
            if (entityType is null)
                continue;

            var filter = entityType.GetQueryFilter();
            filter.Should().NotBeNull($"'{scopedType.Name}' implements IWorkspaceScoped but has no query filter");
        }
    }

    [Fact]
    public void QueryFilter_WhenWorkspaceSet_FiltersByWorkspaceId()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(Guid.NewGuid(), WorkspaceA, null);
        var model = BuildModel(tenant);

        var boardEntity = model.FindEntityType(typeof(Board));
        var filter = boardEntity!.GetQueryFilter()!;

        var body = Normalize(filter.Body.ToString());
        body.Should().Contain("WorkspaceId", "filter should restrict by WorkspaceId");
        body.Should().NotContain("False", "filter should not block all access");
    }

    [Fact]
    public void QueryFilter_WhenNoWorkspace_BlocksAllAccess()
    {
        var model = BuildModel(null);

        var boardEntity = model.FindEntityType(typeof(Board));
        var filter = boardEntity!.GetQueryFilter()!;

        var body = Normalize(filter.Body.ToString());
        body.Should().Match("*False*", "no workspace context should produce a false filter that blocks all");
    }

    [Fact]
    public void QueryFilter_WhenSystemContext_RemovesWorkspaceConstraint()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        var model = BuildModel(tenant);

        var boardEntity = model.FindEntityType(typeof(Board));
        var filter = boardEntity!.GetQueryFilter();

        filter.Should().NotBeNull("soft-delete filter still applies in system context");
        var body = Normalize(filter!.Body.ToString());

        body.Should().NotContain(WorkspaceA.ToString(), "system context should bypass workspace filter");
        body.Should().NotContain("False", "system context should not block access");
    }

    [Fact]
    public void NonWorkspaceScopedEntities_NoWorkspaceSqlInFilter()
    {
        var model = BuildModel(new FakeCurrentTenantContext());

        var userEntity = model.FindEntityType(typeof(User));
        var filter = userEntity!.GetQueryFilter();

        if (filter is not null)
        {
            var body = Normalize(filter.Body.ToString());
            body.Should().NotContain("WorkspaceId", "User does not implement IWorkspaceScoped");
        }
    }

    [Fact]
    public void QueryFilter_WhenSwitchingWorkspace_ChangesFilter()
    {
        var tenant = new FakeCurrentTenantContext();

        tenant.SetWorkspace(Guid.NewGuid(), WorkspaceA, null);
        var modelA = BuildModel(tenant);
        var boardA = modelA.FindEntityType(typeof(Board))!;
        var bodyA = Normalize(boardA.GetQueryFilter()!.Body.ToString());
        bodyA.Should().Contain("WorkspaceId", "model A should filter by workspace");

        var workspaceB = Guid.Parse("B0000000-0000-0000-0000-000000000002");
        tenant.SetWorkspace(Guid.NewGuid(), workspaceB, null);
        var modelB = BuildModel(tenant);
        var boardB = modelB.FindEntityType(typeof(Board))!;
        var bodyB = Normalize(boardB.GetQueryFilter()!.Body.ToString());
        bodyB.Should().Contain("WorkspaceId", "model B should filter by workspace");
    }

    [Fact]
    public void MultipleBoundedContexts_AllHaveQueryFilters()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(Guid.NewGuid(), WorkspaceA, null);
        var model = BuildModel(tenant);

        var boardFilter = model.FindEntityType(typeof(Board))!.GetQueryFilter();
        var memberFilter = model.FindEntityType(typeof(WorkspaceMember))!.GetQueryFilter();
        var commentFilter = model.FindEntityType(typeof(Comment))!.GetQueryFilter();
        var workspaceFilter = model.FindEntityType(typeof(Workspace))!.GetQueryFilter();

        boardFilter.Should().NotBeNull();
        memberFilter.Should().NotBeNull();
        commentFilter.Should().NotBeNull();
        workspaceFilter.Should().NotBeNull("Workspace extends AggregateRoot which extends SoftDeletableEntity");
        Normalize(workspaceFilter!.Body.ToString()).Should().NotContain("WorkspaceId", "Workspace does not implement IWorkspaceScoped");
    }

    [Fact]
    public void QueryFilter_SoftDeleteAndWorkspace_CombineCorrectly()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(Guid.NewGuid(), WorkspaceA, null);
        var model = BuildModel(tenant);

        var boardEntity = model.FindEntityType(typeof(Board))!;
        var filter = boardEntity.GetQueryFilter()!;

        var body = Normalize(filter.Body.ToString());
        body.Should().Contain("DeletedAt");
        body.Should().Contain("WorkspaceId");
    }

    [Fact]
    public void QueryFilter_WithoutWorkspace_SoftDeleteStillApplies()
    {
        var model = BuildModel(null);

        var boardEntity = model.FindEntityType(typeof(Board))!;
        var filter = boardEntity.GetQueryFilter()!;

        var body = Normalize(filter.Body.ToString());
        body.Should().Contain("DeletedAt");
    }

    private static string Normalize(string input)
    {
        return input
            .Replace(" ", "")
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace("\t", "");
    }

    private static IModel BuildModel(ICurrentTenantContext? tenant)
    {
        var builder = new ModelBuilder();
        using var context = new ModelTestDbContext(tenant);
        context.OnModelCreatingPublic(builder);
        return builder.FinalizeModel();
    }

    private sealed class ModelTestDbContext : ApplicationDbContext
    {
        public ModelTestDbContext(ICurrentTenantContext? tenant)
            : base(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase("test").Options, tenant) { }

        public void OnModelCreatingPublic(ModelBuilder builder) => OnModelCreating(builder);
    }
}
