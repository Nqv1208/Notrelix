using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Pages.Commands.UpdatePage;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.Pages.Events;
using TestDbSet = Notrelix.Application.Tests.Features.Collaboration.TestDbSet;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>Rename: a new title bumps the version once and raises one event; the same title, trimmed, does neither; bad titles change nothing.</summary>
public sealed class UpdatePageHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public async Task Rename_ToANewTitle_BumpsTheVersionOnce_AndRaisesOneEvent()
    {
        var page = NewPage("Plan");
        var before = page.Version;

        var result = await Sut(page).Handle(new UpdatePageCommand(page.Id, "  Roadmap ", before), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        page.Title.Should().Be("Roadmap", "the title is trimmed");
        page.Version.Should().Be(before + 1);
        page.DomainEvents.OfType<PageRenamedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task Rename_ToTheSameTitle_IsANoOp_AndRetryAfterSuccessChangesNothing()
    {
        var page = NewPage("Plan");
        var sut = Sut(page);
        await sut.Handle(new UpdatePageCommand(page.Id, "Roadmap", page.Version), CancellationToken.None);
        var version = page.Version;
        var events = page.DomainEvents.Count;

        var retry = await sut.Handle(new UpdatePageCommand(page.Id, " Roadmap ", version), CancellationToken.None);

        retry.Succeeded.Should().BeTrue();
        page.Version.Should().Be(version);
        page.DomainEvents.Should().HaveCount(events);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Rename_ToAnEmptyTitle_IsRefused_AndTheTitleStays(string title)
    {
        var page = NewPage("Plan");

        var act = () => Sut(page).Handle(new UpdatePageCommand(page.Id, title, page.Version), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        page.Title.Should().Be("Plan");
    }

    [Fact]
    public async Task Rename_ToATitleOver500Characters_IsRefused_AndAnExactly500IsAllowed()
    {
        var page = NewPage("Plan");

        var tooLong = () => Sut(page).Handle(new UpdatePageCommand(page.Id, new string('x', 501), page.Version), CancellationToken.None);
        await tooLong.Should().ThrowAsync<Exception>();
        page.Title.Should().Be("Plan");

        await Sut(page).Handle(new UpdatePageCommand(page.Id, new string('x', 500), page.Version), CancellationToken.None);
        page.Title.Should().HaveLength(500);
    }

    [Fact]
    public async Task Rename_OfAMissingPage_IsNotFound()
    {
        var page = NewPage("Plan");

        var act = () => Sut(page).Handle(new UpdatePageCommand(Guid.CreateVersion7(), "Roadmap", 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static Page NewPage(string title) => Page.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), title, UserId, Now);

    private static UpdatePageCommandHandler Sut(Page page)
    {
        var context = new Mock<IDocumentDbContext>();
        context.Setup(c => c.Pages).Returns(TestDbSet.Create([page]).Object);
        var user = new Mock<ICurrentUser>();
        user.Setup(u => u.UserId).Returns(UserId);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(Now.AddMinutes(1));
        return new UpdatePageCommandHandler(context.Object, user.Object, clock.Object);
    }
}
