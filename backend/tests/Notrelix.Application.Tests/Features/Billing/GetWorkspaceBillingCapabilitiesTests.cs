using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Billing.Entitlements.Queries.GetWorkspaceBillingCapabilities;
using Notrelix.Application.Features.Billing.Public.Facts;

namespace Notrelix.Application.Tests.Features.Billing;

public class GetWorkspaceBillingCapabilitiesTests
{
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();

    private readonly Mock<IBillingCapabilityFacts> _facts = new();
    private readonly Mock<ICurrentRequestContext> _requestContext = new();

    private GetWorkspaceBillingCapabilitiesQueryHandler CreateSut()
    {
        _requestContext.Setup(c => c.RequireAccountId()).Returns(AccountId);
        return new GetWorkspaceBillingCapabilitiesQueryHandler(_facts.Object, _requestContext.Object, new Mock<IBillingUsageReader>().Object);
    }

    private void SetFact(BillingCapabilityFact? fact) =>
        _facts
            .Setup(f => f.GetCapabilityAsync(
                AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fact);

    private async Task<BillingCapabilityDto> Run()
    {
        var result = await CreateSut().Handle(new GetWorkspaceBillingCapabilitiesQuery(WorkspaceId), CancellationToken.None);
        Assert.Equal(BillingCapabilityCode.All, result.Data!.Select(c => c.Code));
        return result.Data!.Single(c => c.Code == BillingCapabilityCode.AutomationRule);
    }

    [Fact]
    public async Task A_finite_grant_reports_its_limit_usage_and_remaining()
    {
        SetFact(new BillingCapabilityFact(true, 5, 2, 3));

        var dto = await Run();

        Assert.Equal(BillingCapabilityCode.AutomationRule, dto.Code);
        Assert.True(dto.IsGranted);
        Assert.False(dto.IsUnlimited);
        Assert.Equal(5, dto.Limit);
        Assert.Equal(2, dto.Used);
        Assert.Equal(3, dto.Remaining);
    }

    [Fact]
    public async Task A_full_grant_is_still_granted()
    {
        SetFact(new BillingCapabilityFact(true, 2, 2, 0));

        var dto = await Run();

        Assert.True(dto.IsGranted);
        Assert.Equal(0, dto.Remaining);
    }

    [Fact]
    public async Task An_unlimited_grant_has_no_limit()
    {
        SetFact(new BillingCapabilityFact(true, null, 4, null));

        var dto = await Run();

        Assert.True(dto.IsGranted);
        Assert.True(dto.IsUnlimited);
        Assert.Null(dto.Limit);
        Assert.Equal(4, dto.Used);
    }

    [Fact]
    public async Task A_missing_grant_is_not_granted_but_keeps_the_ledger_usage()
    {
        SetFact(new BillingCapabilityFact(false, null, 3, null));

        var dto = await Run();

        Assert.False(dto.IsGranted);
        Assert.False(dto.IsUnlimited);
        Assert.Equal(3, dto.Used);
    }
}
