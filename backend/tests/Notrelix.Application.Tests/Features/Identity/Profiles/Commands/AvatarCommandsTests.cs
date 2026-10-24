using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.Identity.Profiles.Commands.RemoveAvatar;
using Notrelix.Application.Features.Identity.Profiles.Commands.UploadAvatar;
using Notrelix.Application.Features.Identity.Profiles.Services;

namespace Notrelix.Application.Tests.Features.Identity.Profiles.Commands;

public sealed class AvatarCommandsTests : IdentityHandlerTestBase
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 1, 2, 3, 4];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 16, 0x4A, 0x46, 0x49, 0x46, 0, 1, 1, 0, 0, 1];
    private static readonly byte[] Webp = [.. "RIFF"u8, 0x24, 0, 0, 0, .. "WEBPVP8 "u8, 1, 2, 3, 4];

    private sealed class MemoryStorage : IStorageService
    {
        public Dictionary<string, byte[]> Objects { get; } = new();
        public Dictionary<string, string> ContentTypes { get; } = new();

        public async Task WriteAsync(string key, Stream content, string contentType, CancellationToken ct)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct); // a stream exception (size cap) escapes, nothing remains
            Objects[key] = buffer.ToArray();
            ContentTypes[key] = contentType;
        }

        public Task<Stream?> OpenReadAsync(string key, CancellationToken ct) =>
            Task.FromResult<Stream?>(Objects.TryGetValue(key, out var bytes) ? new MemoryStream(bytes) : null);

        public Task DeleteAsync(string key, CancellationToken ct)
        {
            Objects.Remove(key);
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<StorageObjectInfo> ListAsync(
            string prefix, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await Task.CompletedTask;
            foreach (var key in Objects.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
            {
                yield return new StorageObjectInfo(key, DateTimeOffset.UtcNow);
            }
        }
    }

    private readonly MemoryStorage _storage = new();
    private readonly RequestRollbackCompensations _compensations = new(NullLogger<RequestRollbackCompensations>.Instance);

    private UploadAvatarCommandHandler Upload() =>
        new(IdentityContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, _storage, _compensations);

    private RemoveAvatarCommandHandler Remove() =>
        new(IdentityContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

    [Theory]
    [InlineData("png", "image/png")]
    [InlineData("jpg", "image/jpeg")]
    [InlineData("webp", "image/webp")]
    public async Task Upload_StoresTheImage_UnderAnOwnerPrefixedKey_AndPointsTheUserAtIt(string extension, string contentType)
    {
        var user = CreateUser();
        SetupUsers(user);
        var bytes = extension switch { "png" => Png, "jpg" => Jpeg, _ => Webp };

        var result = await Upload().Handle(new UploadAvatarCommand(new MemoryStream(bytes)), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        user.AvatarStorageKey.Should().NotBeNull();
        AvatarStorageKey.BelongsTo(user.AvatarStorageKey!, TestUserId).Should().BeTrue();
        user.AvatarStorageKey.Should().EndWith("." + extension);
        _storage.Objects[user.AvatarStorageKey!].Should().Equal(bytes);
        _storage.ContentTypes[user.AvatarStorageKey!].Should().Be(contentType);
        result.Data!.AvatarUrl.Should().Be(user.AvatarUrl);
        result.Data.AvatarUrl.Should().StartWith($"/api/v1/users/{TestUserId}/avatar?v=");
    }

    [Fact]
    public async Task Upload_DerivesTheFormatFromTheBytes_NotFromAnythingTheClientDeclares()
    {
        SetupUsers(CreateUser());
        byte[] gif = [.. "GIF89a"u8, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0];
        byte[] svg = [.. "<svg xmlns='http://www.w3.org/2000/svg'/>"u8];
        byte[] exe = [0x4D, 0x5A, 0x90, 0, 3, 0, 0, 0, 4, 0, 0, 0, 0xFF, 0xFF, 0, 0];

        foreach (var body in new[] { gif, svg, exe, Array.Empty<byte>(), new byte[] { 1, 2, 3 } })
        {
            var upload = () => Upload().Handle(new UploadAvatarCommand(new MemoryStream(body)), CancellationToken.None);
            var thrown = (await upload.Should().ThrowAsync<UnsupportedMediaTypeException>()).Which;
            thrown.ErrorCode.Should().Be(UploadAvatarCommandHandler.UnsupportedErrorCode);
        }

        _storage.Objects.Should().BeEmpty();
    }

    [Fact]
    public async Task Upload_RefusesAnImageOverTheCap_WithAStableCode_AndRegistersTheObjectForCleanup()
    {
        var user = CreateUser();
        SetupUsers(user);
        var oversized = new byte[AvatarImage.MaxBytes + 1];
        Png.CopyTo(oversized, 0);

        var upload = () => Upload().Handle(new UploadAvatarCommand(new MemoryStream(oversized)), CancellationToken.None);

        var thrown = (await upload.Should().ThrowAsync<PayloadTooLargeException>()).Which;
        thrown.ErrorCode.Should().Be(UploadAvatarCommandHandler.TooLargeErrorCode);
        thrown.MaxBytes.Should().Be(AvatarImage.MaxBytes);
        user.AvatarStorageKey.Should().BeNull();
        user.AvatarUrl.Should().BeNull();
        _storage.Objects.Should().BeEmpty();
    }

    [Fact]
    public async Task Upload_AtExactlyTheCap_IsAccepted()
    {
        var user = CreateUser();
        SetupUsers(user);
        var exact = new byte[AvatarImage.MaxBytes];
        Png.CopyTo(exact, 0);

        var result = await Upload().Handle(new UploadAvatarCommand(new MemoryStream(exact)), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _storage.Objects[user.AvatarStorageKey!].Length.Should().Be((int)AvatarImage.MaxBytes);
    }

    [Fact]
    public async Task Upload_ReplacingAPhoto_PointsAtTheNewObject_AndLeavesTheOldOneForTheSweep()
    {
        var user = CreateUser();
        SetupUsers(user);
        await Upload().Handle(new UploadAvatarCommand(new MemoryStream(Png)), CancellationToken.None);
        var first = user.AvatarStorageKey!;

        await Upload().Handle(new UploadAvatarCommand(new MemoryStream(Jpeg)), CancellationToken.None);

        user.AvatarStorageKey.Should().NotBe(first).And.EndWith(".jpg");
        _storage.Objects.Should().ContainKey(first); // removed by the orphan sweep, never before commit
    }

    [Fact]
    public async Task Upload_ForAnUnknownUser_IsNotFound_AndWritesNothing()
    {
        SetupUsers();

        var result = await Upload().Handle(new UploadAvatarCommand(new MemoryStream(Png)), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        _storage.Objects.Should().BeEmpty();
    }

    [Fact]
    public async Task Remove_ClearsThePhoto_AndIsIdempotent()
    {
        var user = CreateUser();
        SetupUsers(user);
        await Upload().Handle(new UploadAvatarCommand(new MemoryStream(Png)), CancellationToken.None);

        var first = await Remove().Handle(new RemoveAvatarCommand(), CancellationToken.None);
        var version = user.Version;
        var second = await Remove().Handle(new RemoveAvatarCommand(), CancellationToken.None);

        first.Succeeded.Should().BeTrue();
        first.Data!.AvatarUrl.Should().BeNull();
        second.Succeeded.Should().BeTrue();
        user.AvatarUrl.Should().BeNull();
        user.AvatarStorageKey.Should().BeNull();
        user.Version.Should().Be(version);
    }

    [Fact]
    public void StorageKey_IsServerShaped_AndRejectsAnythingElse()
    {
        var key = AvatarStorageKey.Create(TestUserId, new AvatarFormat("image/png", "png"));

        AvatarStorageKey.IsAvatarKey(key).Should().BeTrue();
        AvatarStorageKey.BelongsTo(key, TestUserId).Should().BeTrue();
        AvatarStorageKey.BelongsTo(key, Guid.NewGuid()).Should().BeFalse();
        AvatarStorageKey.IsAvatarKey($"{Guid.NewGuid():N}/{Guid.NewGuid():N}/attachments/{Guid.NewGuid():N}").Should().BeFalse();
        AvatarStorageKey.IsAvatarKey($"users/{TestUserId:N}/avatar/../x.png").Should().BeFalse();
        AvatarStorageKey.IsAvatarKey($"users/{TestUserId:N}/avatar/{Guid.NewGuid():N}.svg").Should().BeFalse();
    }
}
