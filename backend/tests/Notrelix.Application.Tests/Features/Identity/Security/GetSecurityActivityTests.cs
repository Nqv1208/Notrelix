using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.Queries.GetSecurityActivity;
using Notrelix.Domain.Identity.Security;

namespace Notrelix.Application.Tests.Features.Identity.Security;

public class GetSecurityActivityTests : IdentityHandlerTestBase
{
    private readonly Mock<ISecurityActivityReader> _reader = new();

    private GetSecurityActivityQueryHandler CreateSut() => new(_reader.Object, RequestContextMock.Object);

    private SecurityActivityDto Entry(int minutesAgo) => new(
        Guid.CreateVersion7(), SecurityActivityType.SignInSucceeded, TestNow.AddMinutes(-minutesAgo), "203.0.113.7", "UA", null);

    [Fact]
    public async Task Handle_ReadsOnlyTheCurrentUsersFeed()
    {
        _reader.Setup(r => r.ListAsync(TestUserId, null, 21, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Entry(1)]);

        var result = await CreateSut().Handle(new GetSecurityActivityQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.NextCursor.Should().BeNull();
        _reader.Verify(r => r.ListAsync(
            It.Is<Guid>(id => id != TestUserId), It.IsAny<SecurityActivityPosition?>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMoreRowsExist_ReturnsACursorOfTheLastItemOnThePage()
    {
        var rows = new[] { Entry(1), Entry(2), Entry(3) };
        _reader.Setup(r => r.ListAsync(TestUserId, null, 3, It.IsAny<CancellationToken>())).ReturnsAsync(rows);

        var result = await CreateSut().Handle(new GetSecurityActivityQuery(Limit: 2), CancellationToken.None);

        result.Data!.Items.Should().Equal(rows[0], rows[1]);
        result.Data.NextCursor.Should().NotBeNullOrEmpty();

        SecurityActivityPosition? seen = null;
        _reader.Setup(r => r.ListAsync(TestUserId, It.IsAny<SecurityActivityPosition?>(), 3, It.IsAny<CancellationToken>()))
            .Callback<Guid, SecurityActivityPosition?, int, CancellationToken>((_, after, _, _) => seen = after)
            .ReturnsAsync([rows[2]]);

        var next = await CreateSut().Handle(
            new GetSecurityActivityQuery(result.Data.NextCursor, 2), CancellationToken.None);

        seen.Should().Be(new SecurityActivityPosition(rows[1].OccurredAt, rows[1].Id));
        next.Data!.Items.Should().Equal(rows[2]);
        next.Data.NextCursor.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validator_RejectsLimitsOutsideTheBounds(int limit)
    {
        new GetSecurityActivityQueryValidator().Validate(new GetSecurityActivityQuery(Limit: limit))
            .IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("not-a-cursor")]
    [InlineData("MTIzOmFiYw")]
    public void Validator_RejectsAMalformedCursor(string cursor)
    {
        new GetSecurityActivityQueryValidator().Validate(new GetSecurityActivityQuery(Cursor: cursor))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_AcceptsNoCursorAndTheDefaultLimit()
    {
        new GetSecurityActivityQueryValidator().Validate(new GetSecurityActivityQuery()).IsValid.Should().BeTrue();
    }
}
