namespace Notrelix.Application.Tests.Common.Idempotency;

public class IdempotencyExecutionContextTests
{
    private static IdempotencyExecutionContext Create() => new();

    [Fact]
    public void RequireKey_BeforeSet_Throws()
    {
        var context = Create();

        var act = () => context.RequireKey();

        act.Should().Throw<IdempotencyContextMissingException>()
            .WithMessage("*No idempotency execution key is set*");
    }

    [Fact]
    public void Set_ValidKey_RequireKey_ReturnsKey()
    {
        var context = Create();

        context.Set("valid-key-123", IdempotencyExecutionSource.Http);

        context.RequireKey().Should().Be("valid-key-123");
        context.Source.Should().Be(IdempotencyExecutionSource.Http);
    }

    [Fact]
    public void Set_KeyShorterThanMinimum_Throws()
    {
        var context = Create();

        var act = () => context.Set("short", IdempotencyExecutionSource.Http);

        act.Should().Throw<ArgumentException>()
            .WithMessage($"*between {IdempotencyExecutionContext.MinKeyLength} and {IdempotencyExecutionContext.MaxKeyLength}*");

        var require = () => context.RequireKey();
        require.Should().Throw<IdempotencyContextMissingException>();
    }

    [Fact]
    public void Set_KeyLongerThanMaximum_Throws()
    {
        var context = Create();

        var act = () => context.Set(new string('k', 129), IdempotencyExecutionSource.Http);

        act.Should().Throw<ArgumentException>();

        var require = () => context.RequireKey();
        require.Should().Throw<IdempotencyContextMissingException>();
    }

    [Fact]
    public void Set_KeyWithControlCharacters_Throws()
    {
        var context = Create();

        var act = () => context.Set("valid-key\n123", IdempotencyExecutionSource.Http);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*control characters*");
    }

    [Fact]
    public void Set_KeyWithLeadingWhitespace_Throws()
    {
        var context = Create();

        var act = () => context.Set(" valid-key-123", IdempotencyExecutionSource.Http);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*leading or trailing whitespace*");
    }

    [Fact]
    public void Set_KeyWithTrailingWhitespace_Throws()
    {
        var context = Create();

        var act = () => context.Set("valid-key-123 ", IdempotencyExecutionSource.Http);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*leading or trailing whitespace*");
    }

    [Fact]
    public void Set_TwoDifferentKeys_InSameScope_Throws()
    {
        var context = Create();
        context.Set("first-key-123", IdempotencyExecutionSource.Http);

        var act = () => context.Set("second-key-456", IdempotencyExecutionSource.Message);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot set two different idempotency keys*");
        context.RequireKey().Should().Be("first-key-123");
    }

    [Fact]
    public void Set_SameKeyTwice_IsAllowed()
    {
        var context = Create();
        context.Set("valid-key-123", IdempotencyExecutionSource.Http);

        var act = () => context.Set("valid-key-123", IdempotencyExecutionSource.Http);

        act.Should().NotThrow();
    }

    [Fact]
    public void Set_SecondCall_UpdatesSource()
    {
        var context = Create();

        context.Set("valid-key-123", IdempotencyExecutionSource.Http);
        context.Set("valid-key-123", IdempotencyExecutionSource.Message);

        context.Source.Should().Be(IdempotencyExecutionSource.Message);
    }

    [Fact]
    public void MarkReplay_SetsIsReplay()
    {
        var context = Create();

        context.MarkReplay();

        context.IsReplay.Should().BeTrue();
    }

    [Fact]
    public void FreshContext_IsNotReplay_AndSourceIsInternal()
    {
        var context = Create();

        context.IsReplay.Should().BeFalse();
        context.Source.Should().Be(IdempotencyExecutionSource.Internal);
    }
}
