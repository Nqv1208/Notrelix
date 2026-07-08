namespace Notrelix.Application.Tests.Behaviors;

public class ConcurrencyBehaviorTests
{
    private static readonly Guid ResourceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public sealed record VersionedCommand : IRequest<string>, IExpectedVersionRequest
    {
        public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, ResourceId);
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

    private static Mock<ILogger<ConcurrencyBehavior<VersionedCommand, string>>> CreateLogger()
    {
        return new Mock<ILogger<ConcurrencyBehavior<VersionedCommand, string>>>();
    }

    [Fact]
    public async Task NonVersionedRequest_ShouldSkipCheck()
    {
        var reader = CreateReader();
        var logger = new Mock<ILogger<ConcurrencyBehavior<UnversionedCommand, string>>>();
        var behavior = new ConcurrencyBehavior<UnversionedCommand, string>(reader.Object, logger.Object);

        var result = await behavior.Handle(new UnversionedCommand(), _ => Task.FromResult("ok"), default);

        result.Should().Be("ok");
        reader.Verify(x => x.GetVersionAsync(It.IsAny<ResourceRef>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExpectedVersionZero_ShouldSkipCheck()
    {
        var reader = CreateReader();
        var logger = CreateLogger();
        var behavior = new ConcurrencyBehavior<VersionedCommand, string>(reader.Object, logger.Object);

        var result = await behavior.Handle(
            new VersionedCommand { ExpectedVersion = 0 },
            _ => Task.FromResult("ok"),
            default);

        result.Should().Be("ok");
        reader.Verify(x => x.GetVersionAsync(It.IsAny<ResourceRef>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MatchingVersion_ShouldProceed()
    {
        var reader = CreateReader(version: 1);
        var logger = CreateLogger();
        var behavior = new ConcurrencyBehavior<VersionedCommand, string>(reader.Object, logger.Object);

        var result = await behavior.Handle(
            new VersionedCommand { ExpectedVersion = 1 },
            _ => Task.FromResult("ok"),
            default);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task VersionMismatch_ShouldThrowConflict()
    {
        var reader = CreateReader(version: 2);
        var logger = CreateLogger();
        var behavior = new ConcurrencyBehavior<VersionedCommand, string>(reader.Object, logger.Object);

        var act = () => behavior.Handle(
            new VersionedCommand { ExpectedVersion = 1 },
            _ => Task.FromResult("ok"),
            default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage($"*version mismatch*");
    }

    [Fact]
    public async Task NullVersion_ShouldProceed()
    {
        var reader = CreateReader(version: null);
        var logger = CreateLogger();
        var behavior = new ConcurrencyBehavior<VersionedCommand, string>(reader.Object, logger.Object);

        var result = await behavior.Handle(
            new VersionedCommand { ExpectedVersion = 1 },
            _ => Task.FromResult("ok"),
            default);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task UnsupportedResourceType_ShouldLogWarning_AndProceed()
    {
        var reader = new Mock<IResourceVersionReader>();
        reader.Setup(x => x.GetVersionAsync(It.IsAny<ResourceRef>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSupportedException("ResourceType 'Widget' not supported"));
        var logger = CreateLogger();
        var behavior = new ConcurrencyBehavior<VersionedCommand, string>(reader.Object, logger.Object);

        var result = await behavior.Handle(
            new VersionedCommand { ExpectedVersion = 1 },
            _ => Task.FromResult("ok"),
            default);

        result.Should().Be("ok");
    }
}
