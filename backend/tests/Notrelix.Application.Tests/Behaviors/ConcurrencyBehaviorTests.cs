namespace Notrelix.Application.Tests.Behaviors;

public class ConcurrencyBehaviorTests
{
    private static readonly Guid ResourceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public sealed record VersionedCommand : IRequest<string>, IExpectedVersionRequest
    {
        public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), ResourceId);
        public long ExpectedVersion { get; init; } = 1;
    }

    public sealed record UnversionedCommand : IRequest<string>;

    private static Mock<IResourceVersionReader> CreateReader(long? version = 1)
    {
        var reader = new Mock<IResourceVersionReader>();
        reader.Setup(x => x.GetVersionAsync(It.IsAny<ResourceRef>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);
        return reader;
    }

    [Fact]
    public async Task NonVersionedRequest_ShouldSkipCheck()
    {
        var reader = CreateReader();
        var behavior = new ConcurrencyBehavior<UnversionedCommand, string>(reader.Object);

        var result = await behavior.Handle(new UnversionedCommand(), _ => Task.FromResult("ok"), default);

        result.Should().Be("ok");
        reader.Verify(x => x.GetVersionAsync(It.IsAny<ResourceRef>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExpectedVersionZero_ShouldThrowValidationException()
    {
        var reader = CreateReader();
        var behavior = new ConcurrencyBehavior<VersionedCommand, string>(reader.Object);

        var act = () => behavior.Handle(
            new VersionedCommand { ExpectedVersion = 0 },
            _ => Task.FromResult("ok"),
            default);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage($"*ExpectedVersion must be a positive value*");
        reader.Verify(x => x.GetVersionAsync(It.IsAny<ResourceRef>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExpectedVersionNegative_ShouldThrowValidationException()
    {
        var reader = CreateReader();
        var behavior = new ConcurrencyBehavior<VersionedCommand, string>(reader.Object);

        var act = () => behavior.Handle(
            new VersionedCommand { ExpectedVersion = -1 },
            _ => Task.FromResult("ok"),
            default);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage($"*ExpectedVersion must be a positive value*");
    }

    [Fact]
    public async Task MatchingVersion_ShouldProceed()
    {
        var reader = CreateReader(version: 1);
        var behavior = new ConcurrencyBehavior<VersionedCommand, string>(reader.Object);

        var result = await behavior.Handle(
            new VersionedCommand { ExpectedVersion = 1 },
            _ => Task.FromResult("ok"),
            default);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task VersionMismatch_ShouldThrowPreconditionFailed()
    {
        var reader = CreateReader(version: 2);
        var behavior = new ConcurrencyBehavior<VersionedCommand, string>(reader.Object);

        var act = () => behavior.Handle(
            new VersionedCommand { ExpectedVersion = 1 },
            _ => Task.FromResult("ok"),
            default);

        await act.Should().ThrowAsync<PreconditionFailedException>()
            .WithMessage($"*version mismatch*");
    }

    [Fact]
    public async Task NullVersion_ShouldThrowNotFound()
    {
        var reader = CreateReader(version: null);
        var behavior = new ConcurrencyBehavior<VersionedCommand, string>(reader.Object);

        var act = () => behavior.Handle(
            new VersionedCommand { ExpectedVersion = 1 },
            _ => Task.FromResult("ok"),
            default);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*not found*");
    }

    [Fact]
    public async Task UnsupportedResourceType_ShouldThrow()
    {
        var reader = new Mock<IResourceVersionReader>();
        reader.Setup(x => x.GetVersionAsync(It.IsAny<ResourceRef>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSupportedException("ResourceKind 'Widget' not supported"));
        var behavior = new ConcurrencyBehavior<VersionedCommand, string>(reader.Object);

        var act = () => behavior.Handle(
            new VersionedCommand { ExpectedVersion = 1 },
            _ => Task.FromResult("ok"),
            default);

        await act.Should().ThrowAsync<NotSupportedException>();
    }
}
