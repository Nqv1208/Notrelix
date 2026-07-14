using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Workspaces.Invitations;

namespace Notrelix.Application.Tests.Features.Workspaces;

public abstract class WorkspaceHandlerTestBase
{
    protected readonly Mock<IWorkspaceDbContext> DbContextMock = new();
    protected readonly Mock<ICurrentRequestContext> RequestContextMock = new();
    protected readonly Mock<IDateTimeProvider> DateTimeProviderMock = new();

    protected readonly Guid TestAccountId = Guid.CreateVersion7();
    protected readonly Guid TestWorkspaceId = Guid.CreateVersion7();
    protected readonly Guid TestUserId = Guid.CreateVersion7();
    protected readonly DateTimeOffset TestNow = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    protected WorkspaceHandlerTestBase()
    {
        RequestContextMock.Setup(c => c.UserId).Returns(TestUserId);
        RequestContextMock.Setup(c => c.RequireAccountId()).Returns(TestAccountId);
        RequestContextMock.Setup(c => c.RequireWorkspaceId()).Returns(TestWorkspaceId);
        DateTimeProviderMock.Setup(c => c.UtcNow).Returns(TestNow);

        DbContextMock.Setup(c => c.Workspaces).Returns(CreateAsyncDbSet(new List<Workspace>()));
        DbContextMock.Setup(c => c.WorkspaceMembers).Returns(CreateAsyncDbSet(new List<WorkspaceMember>()));
        DbContextMock.Setup(c => c.WorkspaceInvitations).Returns(CreateAsyncDbSet(new List<WorkspaceInvitation>()));
        DbContextMock.Setup(c => c.Spaces).Returns(CreateAsyncDbSet(new List<Space>()));
        DbContextMock.Setup(c => c.Teams).Returns(CreateAsyncDbSet(new List<Team>()));
        DbContextMock.Setup(c => c.TeamMembers).Returns(CreateAsyncDbSet(new List<TeamMember>()));
    }

    protected void SetupWorkspaces(params Workspace[] workspaces) =>
        DbContextMock.Setup(c => c.Workspaces).Returns(CreateAsyncDbSet(workspaces.ToList()));

    protected void SetupMembers(params WorkspaceMember[] members) =>
        DbContextMock.Setup(c => c.WorkspaceMembers).Returns(CreateAsyncDbSet(members.ToList()));

    protected void SetupInvitations(params WorkspaceInvitation[] invitations) =>
        DbContextMock.Setup(c => c.WorkspaceInvitations).Returns(CreateAsyncDbSet(invitations.ToList()));

    protected void SetupSpaces(params Space[] spaces) =>
        DbContextMock.Setup(c => c.Spaces).Returns(CreateAsyncDbSet(spaces.ToList()));

    protected void SetupTeams(params Team[] teams) =>
        DbContextMock.Setup(c => c.Teams).Returns(CreateAsyncDbSet(teams.ToList()));

    protected void SetupTeamMembers(params TeamMember[] teamMembers) =>
        DbContextMock.Setup(c => c.TeamMembers).Returns(CreateAsyncDbSet(teamMembers.ToList()));

    protected Workspace CreateWorkspace(Guid? id = null, bool isArchived = false)
    {
        var ws = Workspace.Create(TestAccountId, TestUserId, "Test Workspace", "test-workspace", TestNow, "Description");
        if (id.HasValue)
            ws.GetType().GetProperty(nameof(Workspace.Id))!.SetValue(ws, id.Value);
        if (isArchived)
            ws.Archive(TestUserId, TestNow.AddDays(1));
        return ws;
    }

    protected Space CreateSpace(SpaceVisibility visibility = SpaceVisibility.Workspace, SpaceType spaceType = SpaceType.Folder)
        => Space.Create(TestAccountId, TestWorkspaceId, "Test Space", visibility, TestUserId, TestNow, spaceType);

    protected Team CreateTeam(Guid? id = null)
    {
        var team = Team.Create(TestAccountId, TestWorkspaceId, "Test Team", TestUserId, TestNow);
        if (id.HasValue)
            team.GetType().GetProperty(nameof(Team.Id))!.SetValue(team, id.Value);
        return team;
    }

    protected WorkspaceMember CreateMember(WorkspaceRole role = WorkspaceRole.Member, Guid? userId = null)
        => WorkspaceMember.Create(TestAccountId, TestWorkspaceId, userId ?? TestUserId, role, TestUserId, TestNow);

    protected TeamMember CreateTeamMember(Guid? teamId = null, TeamMemberRole role = TeamMemberRole.Member, Guid? userId = null)
        => TeamMember.Create(TestAccountId, TestWorkspaceId, teamId ?? Guid.CreateVersion7(), userId ?? TestUserId, role, TestUserId, TestNow);

    private static DbSet<T> CreateAsyncDbSet<T>(List<T> data) where T : class
    {
        var mock = new Mock<DbSet<T>>();
        var queryable = data.AsQueryable();

        mock.As<IAsyncEnumerable<T>>()
            .Setup(s => s.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));

        mock.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(queryable));

        mock.As<IQueryable<T>>()
            .Setup(m => m.Expression)
            .Returns(queryable.Expression);

        mock.As<IQueryable<T>>()
            .Setup(m => m.ElementType)
            .Returns(queryable.ElementType);

        mock.As<IQueryable<T>>()
            .Setup(m => m.GetEnumerator())
            .Returns(() => data.GetEnumerator());

        mock.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(data.Add);
        mock.Setup(m => m.AddRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>(items => data.AddRange(items));

        return mock.Object;
    }
}

// ── Async query infrastructure ────────────────────────────────

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
    public T Current => _inner.Current;
    public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());
}

internal class TestAsyncQueryProvider<T> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    public TestAsyncQueryProvider(IQueryable<T> inner) => _inner = inner.Provider;

    public IQueryable CreateQuery(Expression expression)
    {
        var queryable = _inner.CreateQuery<T>(expression);
        return new TestAsyncEnumerable<T>(queryable);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        var queryable = _inner.CreateQuery<TElement>(expression);
        return new TestAsyncEnumerable<TElement>(queryable);
    }

    public object Execute(Expression expression) => _inner.Execute(expression)!;

    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult);

        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var innerType = resultType.GenericTypeArguments[0];

            var executeMethod = typeof(TestAsyncQueryProvider<T>)
                .GetMethod(nameof(ExecuteSync), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(innerType);

            var taskResult = executeMethod.Invoke(this, [expression]);
            return (TResult)taskResult!;
        }

        throw new NotSupportedException($"Unsupported async result type: {resultType}");
    }

    private Task<TResult> ExecuteSync<TResult>(Expression expression)
    {
        var result = _inner.Execute(expression);
        return Task.FromResult((TResult)result!);
    }
}

internal class TestAsyncEnumerable<T> : IAsyncEnumerable<T>, IQueryable<T>
{
    private readonly IQueryable<T> _queryable;

    public TestAsyncEnumerable(IQueryable<T> queryable)
    {
        _queryable = queryable;
        Provider = new TestAsyncQueryProvider<T>(queryable);
    }

    public Type ElementType => _queryable.ElementType;
    public Expression Expression => _queryable.Expression;
    public IQueryProvider Provider { get; }
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(_queryable.GetEnumerator());
    public IEnumerator<T> GetEnumerator() => _queryable.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _queryable.GetEnumerator();
}
