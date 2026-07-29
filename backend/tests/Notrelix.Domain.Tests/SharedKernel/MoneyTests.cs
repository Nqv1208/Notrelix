using FluentAssertions;

namespace Notrelix.Domain.Tests.SharedKernel;

public class MoneyTests
{
    [Fact]
    public void Create_ShouldSetAmountAndCurrency()
    {
        var money = Money.Create(99.99m, "USD");
        money.Amount.Should().Be(99.99m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Currency_ShouldBeUpperCase()
    {
        var money = Money.Create(10m, "usd");
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Currency_ShouldTrim()
    {
        var money = Money.Create(10m, "  usd  ");
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Currency_InvalidFormat_ShouldThrow()
    {
        var act = () => Money.Create(10m, "US");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Currency_TooLong_ShouldThrow()
    {
        var act = () => Money.Create(10m, "USDD");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Currency_Numeric_ShouldThrow()
    {
        var act = () => Money.Create(10m, "123");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Currency_Empty_ShouldThrow()
    {
        var act = () => Money.Create(10m, "");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void NegativeAmount_ShouldBeAllowed()
    {
        var money = Money.Create(-50m, "USD");
        money.Amount.Should().Be(-50m);
    }

    [Fact]
    public void ZeroAmount_ShouldBeAllowed()
    {
        var money = Money.Create(0m, "USD");
        money.Amount.Should().Be(0m);
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var a = Money.Create(100m, "EUR");
        var b = Money.Create(100m, "EUR");
        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentAmount_ShouldNotBeEqual()
    {
        var a = Money.Create(100m, "EUR");
        var b = Money.Create(200m, "EUR");
        a.Should().NotBe(b);
    }

    [Fact]
    public void Equality_DifferentCurrency_ShouldNotBeEqual()
    {
        var a = Money.Create(100m, "EUR");
        var b = Money.Create(100m, "USD");
        a.Should().NotBe(b);
    }

    [Fact]
    public void ToString_ShouldFormat()
    {
        var money = Money.Create(99.50m, "USD");
        money.ToString().Should().Be("99.50 USD");
    }

    [Fact]
    public void Currency_Lowercase_ShouldNormalize()
    {
        var a = Money.Create(10m, "eur");
        var b = Money.Create(10m, "EUR");
        a.Should().Be(b);
    }
}
