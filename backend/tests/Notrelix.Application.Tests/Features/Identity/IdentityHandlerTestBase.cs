using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Notrelix.Application.Common.Email;
using Notrelix.Application.Common.Messaging;
using Notrelix.Application.Common.RateLimiting;
using Notrelix.Application.Common.Security.Auth;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Verification.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.Identity.Users;
using Notrelix.Application.Features.Identity.Auth.Commands.Login;
using Notrelix.Application.Features.Identity.Auth.Commands.Logout;
using Notrelix.Application.Features.Identity.Auth.Commands.ForgotPassword;
using Notrelix.Application.Features.Identity.Auth.Commands.ResetPassword;

namespace Notrelix.Application.Tests.Features.Identity;

public abstract class IdentityHandlerTestBase
{
    protected readonly Mock<IIdentityDbContext> IdentityContextMock = new();
    protected readonly Mock<IAccountDbContext> AccountContextMock = new();
    protected readonly Mock<IWorkspaceDbContext> WorkspaceContextMock = new();
    protected readonly Mock<IPasswordHasher> PasswordHasherMock = new();
    protected readonly Mock<IAuthSessionIssuer> SessionIssuerMock = new();
    protected readonly Mock<IJwtService> JwtServiceMock = new();
    protected readonly Mock<IJwtBlacklistService> JwtBlacklistMock = new();
    protected readonly Mock<IOtpService> OtpServiceMock = new();
    protected readonly Mock<IRateLimitService> RateLimitServiceMock = new();
    protected readonly Mock<IEmailService> EmailServiceMock = new();
    protected readonly Mock<ICurrentRequestContext> RequestContextMock = new();
    protected readonly Mock<IEmailVerificationTokenIssuer> TokenIssuerMock = new();
    protected readonly Mock<IOneTimeTokenService> OneTimeTokenServiceMock = new();
    protected readonly Mock<IIntegrationEventCollector> IntegrationEventCollectorMock = new();
    protected readonly Mock<IDateTimeProvider> DateTimeProviderMock = new();
    protected readonly Mock<ILogger<LoginCommandHandler>> LoginLoggerMock = new();
    protected readonly Mock<ILogger<LogoutCommandHandler>> LogoutLoggerMock = new();
    protected readonly Mock<ILogger<ForgotPasswordCommandHandler>> ForgotPasswordLoggerMock = new();
    protected readonly Mock<ILogger<ResetPasswordCommandHandler>> ResetPasswordLoggerMock = new();

    protected readonly Guid TestUserId = Guid.CreateVersion7();
    protected readonly string TestEmail = "test@example.com";
    protected readonly string TestPassword = "Password123!";
    protected readonly string TestHashedPassword = "hashed-password";
    protected readonly DateTimeOffset TestNow = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    protected IdentityHandlerTestBase()
    {
        RequestContextMock.Setup(c => c.UserId).Returns(TestUserId);
        RequestContextMock.Setup(c => c.Email).Returns(TestEmail);
        RequestContextMock.Setup(c => c.Name).Returns("Test User");
        RequestContextMock.Setup(c => c.IsAuthenticated).Returns(true);
        RequestContextMock.Setup(c => c.IsSystemContext).Returns(false);

        DateTimeProviderMock.Setup(c => c.UtcNow).Returns(TestNow);

        IdentityContextMock.Setup(c => c.Users).Returns(CreateAsyncDbSet(new List<User>()));
        IdentityContextMock.Setup(c => c.Sessions).Returns(CreateAsyncDbSet(new List<UserSession>()));
        IdentityContextMock.Setup(c => c.OAuthAccounts).Returns(CreateAsyncDbSet(new List<OAuthAccount>()));
        IdentityContextMock.Setup(c => c.EmailVerificationTokens).Returns(CreateAsyncDbSet(new List<EmailVerificationToken>()));

        AccountContextMock.Setup(c => c.Accounts).Returns(CreateAsyncDbSet(new List<Account>()));
        AccountContextMock.Setup(c => c.AccountMembers).Returns(CreateAsyncDbSet(new List<AccountMember>()));

        WorkspaceContextMock.Setup(c => c.Workspaces).Returns(CreateAsyncDbSet(new List<Workspace>()));
        WorkspaceContextMock.Setup(c => c.WorkspaceMembers).Returns(CreateAsyncDbSet(new List<WorkspaceMember>()));
    }

    protected void SetupUsers(params User[] users) =>
        IdentityContextMock.Setup(c => c.Users).Returns(CreateAsyncDbSet(users.ToList()));

    protected void SetupSessions(params UserSession[] sessions) =>
        IdentityContextMock.Setup(c => c.Sessions).Returns(CreateAsyncDbSet(sessions.ToList()));

    protected void SetupEmailVerificationTokens(params EmailVerificationToken[] tokens) =>
        IdentityContextMock.Setup(c => c.EmailVerificationTokens).Returns(CreateAsyncDbSet(tokens.ToList()));

    protected void SetupAccounts(params Account[] accounts) =>
        AccountContextMock.Setup(c => c.Accounts).Returns(CreateAsyncDbSet(accounts.ToList()));

    protected void SetupAccountMembers(params AccountMember[] members) =>
        AccountContextMock.Setup(c => c.AccountMembers).Returns(CreateAsyncDbSet(members.ToList()));

    protected void SetupWorkspaces(params Workspace[] workspaces) =>
        WorkspaceContextMock.Setup(c => c.Workspaces).Returns(CreateAsyncDbSet(workspaces.ToList()));

    protected void SetupWorkspaceMembers(params WorkspaceMember[] members) =>
        WorkspaceContextMock.Setup(c => c.WorkspaceMembers).Returns(CreateAsyncDbSet(members.ToList()));

    protected User CreateUser(
        string? email = null,
        string? name = null,
        UserStatus status = UserStatus.Active,
        bool emailConfirmed = false,
        Guid? id = null)
    {
        var user = User.Create(
            email ?? TestEmail,
            name ?? "Test User",
            TestHashedPassword,
            TestNow);

        user.GetType().GetProperty(nameof(User.Id))!.SetValue(user, id ?? TestUserId);

        if (status != UserStatus.Active)
            user.GetType().GetProperty(nameof(User.Status))!.SetValue(user, status);

        if (emailConfirmed)
            user.ConfirmEmail(null, TestNow);

        return user;
    }

    protected UserSession CreateSession(
        Guid? userId = null,
        SessionStatus status = SessionStatus.Active,
        DateTimeOffset? expiresAt = null,
        string? rawRefreshToken = null)
    {
        var raw = rawRefreshToken ?? $"refresh-token-{Guid.NewGuid()}";
        var token = RefreshTokenHash.Create(raw);
        var session = UserSession.Create(
            userId ?? TestUserId,
            token,
            expiresAt ?? TestNow.AddDays(30),
            TestNow);

        if (status == SessionStatus.Revoked)
            session.Revoke(TestNow);

        return session;
    }

    protected string TestRefreshToken = "test-refresh-token-abc";
    protected string TestRefreshTokenHash => RefreshTokenHash.Create(TestRefreshToken).Hash;

    protected EmailVerificationToken CreateEmailVerificationToken(
        Guid? userId = null,
        string? normalizedEmail = null,
        DateTimeOffset? expiresAt = null)
    {
        var tokenHash = TokenHash.Create($"token-hash-{Guid.NewGuid()}");
        return EmailVerificationToken.Create(
            userId ?? TestUserId,
            tokenHash,
            1,
            normalizedEmail ?? TestEmail.ToLowerInvariant(),
            expiresAt ?? TestNow.AddDays(1),
            TestNow);
    }

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
        var rewritten = new EfToLinqExpressionVisitor().Visit(expression);
        var queryable = _inner.CreateQuery<T>(rewritten);
        return new TestAsyncEnumerable<T>(queryable);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        var rewritten = new EfToLinqExpressionVisitor().Visit(expression);
        var queryable = _inner.CreateQuery<TElement>(rewritten);
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
        var rewritten = new EfToLinqExpressionVisitor().Visit(expression);
        var result = _inner.Execute(rewritten);
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

internal class EfToLinqExpressionVisitor : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(EntityFrameworkQueryableExtensions) &&
            node.Method.Name == nameof(EntityFrameworkQueryableExtensions.AsNoTracking))
        {
            return Visit(node.Arguments[0]);
        }

        return base.VisitMethodCall(node);
    }
}
