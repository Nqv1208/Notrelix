using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.SharedKernel;

public class SharedKernelTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name+tag@domain.co.uk")]
    public void Email_Create_ShouldSucceed_WithValidEmail(string email)
    {
        var result = Email.Create(email);
        result.Value.Should().Be(email.ToLowerInvariant());
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("test@.com")]
    [InlineData("@example.com")]
    public void Email_Create_ShouldThrow_WithInvalidEmail(string email)
    {
        Action act = () => Email.Create(email);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Slug_Create_ShouldSucceed_WithValidSlug()
    {
        var result = Slug.Create("valid-slug-123");
        result.Value.Should().Be("valid-slug-123");
    }

    [Fact]
    public void Slug_Create_ShouldThrow_WithInvalidSlug()
    {
        Action act = () => Slug.Create("Invalid_Slug!");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void DateRange_Create_ShouldSucceed_WhenStartIsBeforeEnd()
    {
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(1);
        
        var range = DateRange.Create(start, end);
        
        range.Start.Should().Be(start);
        range.End.Should().Be(end);
    }

    [Fact]
    public void DateRange_Create_ShouldThrow_WhenStartIsAfterEnd()
    {
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(-1);
        
        Action act = () => DateRange.Create(start, end);
        
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Money_Create_ShouldSucceed_WithValidCurrency()
    {
        var money = Money.Create(100.50m, "usd");
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Create_ShouldThrow_WithInvalidCurrency()
    {
        Action act = () => Money.Create(100, "US");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ResourceRef_Create_ShouldSucceed_WithValidInput()
    {
        var id = Guid.NewGuid();
        var resourceRef = ResourceRef.Create("Board", id);
        
        resourceRef.ResourceType.Should().Be("Board");
        resourceRef.ResourceId.Should().Be(id);
    }
}
