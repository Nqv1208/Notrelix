using Notrelix.Application.Common.Security.Auth;
using Notrelix.Application.Features.Operators.Abstractions;
using Notrelix.Application.Features.Operators.Auth;
using Notrelix.Application.Tests.Features.Billing;
using Notrelix.Domain.Operators;

namespace Notrelix.Application.Tests.Features.Operators;

public class OperatorSignInServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly List<OperatorAccount> _operators = [];
    private readonly Mock<IOperatorsDbContext> _context = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IOperatorTokenIssuer> _tokens = new();
    private readonly Mock<IDateTimeProvider> _clock = new();

    public OperatorSignInServiceTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _context.Setup(c => c.OperatorAccounts).Returns(TestBillingDbSet.Create(_operators).Object);
        _hasher.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hash");
        _hasher.Setup(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns<string, string>((p, h) => p == "correct-horse-battery" && h == "stored");
        _tokens.Setup(t => t.Issue(It.IsAny<OperatorAccount>(), It.IsAny<DateTimeOffset>())).Returns(new OperatorAccessToken("tkn", Now.AddMinutes(30)));
    }

    private OperatorSignInService Service() => new(_context.Object, _hasher.Object, _tokens.Object, _clock.Object);

    private OperatorAccount Add(string email = "ops@notrelix.test")
    {
        var account = OperatorAccount.Create(email, "Ops", "stored", Now.AddDays(-1));
        _operators.Add(account);
        return account;
    }

    [Fact]
    public async Task A_correct_password_issues_a_token_and_clears_failures()
    {
        var account = Add();
        account.RecordFailedSignIn(Now);

        var session = await Service().SignInAsync(" OPS@notrelix.test ", "correct-horse-battery", CancellationToken.None);

        Assert.Equal("tkn", session.AccessToken);
        Assert.Equal(account.Id, session.Operator.Id);
        Assert.Equal(0, account.FailedSignInCount);
        _context.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task A_wrong_password_an_unknown_email_and_a_disabled_operator_get_the_same_answer()
    {
        Add();
        var messages = new List<string>();
        foreach (var (email, password) in new[] { ("ops@notrelix.test", "wrong-password-1"), ("nobody@notrelix.test", "correct-horse-battery") })
        {
            var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => Service().SignInAsync(email, password, CancellationToken.None));
            messages.Add(ex.Message);
        }

        Assert.Single(messages.Distinct());
        _tokens.Verify(t => t.Issue(It.IsAny<OperatorAccount>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [Fact]
    public async Task Five_wrong_passwords_lock_the_operator_even_for_the_right_password()
    {
        var account = Add();
        for (var i = 0; i < OperatorAccount.MaxFailedSignIns; i++)
            await Assert.ThrowsAsync<UnauthorizedException>(() => Service().SignInAsync(account.Email, "wrong-password-1", CancellationToken.None));

        Assert.True(account.IsLockedAt(Now));
        await Assert.ThrowsAsync<UnauthorizedException>(() => Service().SignInAsync(account.Email, "correct-horse-battery", CancellationToken.None));
        _tokens.Verify(t => t.Issue(It.IsAny<OperatorAccount>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [Theory]
    [InlineData(null, "x")]
    [InlineData("", "x")]
    [InlineData("a@b.c", "")]
    public async Task Empty_credentials_are_refused_without_touching_the_store(string? email, string? password)
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => Service().SignInAsync(email, password, CancellationToken.None));
        _context.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
