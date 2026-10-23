using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Common.Storage;
using Notrelix.Infrastructure.Storage;
using Notrelix.Infrastructure.Storage.Providers;

namespace Notrelix.Infrastructure.Tests.Storage;

public sealed class LocalStorageProviderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "notrelix-storage-" + Guid.NewGuid().ToString("N"));
    private readonly LocalStorageProvider _provider;

    public LocalStorageProviderTests()
    {
        _provider = new LocalStorageProvider(Microsoft.Extensions.Options.Options.Create(new StorageOptions { BasePath = _root }));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private const string Key = "acc/ws/attachments/obj1";

    [Fact]
    public async Task Write_Read_Delete_RoundTrip()
    {
        await _provider.WriteAsync(Key, new MemoryStream([1, 2, 3]), "application/octet-stream", default);

        await using (var read = await _provider.OpenReadAsync(Key, default))
        {
            read.Should().NotBeNull();
            using var ms = new MemoryStream();
            await read!.CopyToAsync(ms);
            ms.ToArray().Should().Equal(1, 2, 3);
        }

        await _provider.DeleteAsync(Key, default);
        (await _provider.OpenReadAsync(Key, default)).Should().BeNull();
        await _provider.DeleteAsync(Key, default); // idempotent
    }

    [Fact]
    public async Task Write_StreamExceedingCap_LeavesNothingBehind()
    {
        var capped = new SizeLimitedReadStream(new MemoryStream(new byte[500_000]), 1000);

        var act = () => _provider.WriteAsync(Key, capped, "application/octet-stream", default);

        await act.Should().ThrowAsync<PayloadTooLargeException>();
        (await _provider.OpenReadAsync(Key, default)).Should().BeNull();
        Directory.GetFiles(_root, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task Write_Cancelled_LeavesNothingBehind()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => _provider.WriteAsync(Key, new MemoryStream(new byte[10]), "x", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        Directory.GetFiles(_root, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task Write_ToExistingKey_Fails_AndKeepsOriginal()
    {
        await _provider.WriteAsync(Key, new MemoryStream([1]), "x", default);

        var act = () => _provider.WriteAsync(Key, new MemoryStream([2]), "x", default);

        await act.Should().ThrowAsync<IOException>();
        await using var read = await _provider.OpenReadAsync(Key, default);
        read!.ReadByte().Should().Be(1);
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("a/../../escape")]
    [InlineData("/abs/path")]
    [InlineData("a//b")]
    [InlineData("a/./b")]
    [InlineData("a\\b")]
    [InlineData("a/b c")]
    [InlineData("")]
    [InlineData("..")]
    public async Task PathTraversalAndMalformedKeys_AreRejected_ForEveryOperation(string key)
    {
        await FluentActions.Awaiting(() => _provider.WriteAsync(key, new MemoryStream([1]), "x", default))
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => _provider.OpenReadAsync(key, default))
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => _provider.DeleteAsync(key, default))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task List_ReturnsKeysUnderPrefix_WithLastModified_AndEmptyPrefixListsEverything()
    {
        await _provider.WriteAsync("acc/ws/attachments/o1", new MemoryStream([1]), "application/octet-stream", default);
        await _provider.WriteAsync("acc/ws/attachments/o2", new MemoryStream([2]), "application/octet-stream", default);
        await _provider.WriteAsync("other/ws/attachments/o3", new MemoryStream([3]), "application/octet-stream", default);
        var old = DateTime.UtcNow.AddDays(-3);
        File.SetLastWriteTimeUtc(Path.Combine(_root, "acc", "ws", "attachments", "o1"), old);

        var underAcc = await ToListAsync(_provider.ListAsync("acc", default));
        var all = await ToListAsync(_provider.ListAsync(string.Empty, default));

        underAcc.Select(o => o.Key).Should().BeEquivalentTo("acc/ws/attachments/o1", "acc/ws/attachments/o2");
        underAcc.Single(o => o.Key.EndsWith("o1")).LastModified.Should().BeCloseTo(old, TimeSpan.FromSeconds(2));
        underAcc.Single(o => o.Key.EndsWith("o2")).LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        all.Should().HaveCount(3);
        (await ToListAsync(_provider.ListAsync("missing/prefix", default))).Should().BeEmpty();
    }

    [Fact]
    public async Task List_ReportsAPartialWriteUnderItsFinalKey_AndSkipsForeignFiles()
    {
        Directory.CreateDirectory(Path.Combine(_root, "acc", "ws", "attachments"));
        await File.WriteAllBytesAsync(Path.Combine(_root, "acc", "ws", "attachments", "o9.part"), [1]);
        await File.WriteAllBytesAsync(Path.Combine(_root, "acc", "ws", "attachments", "bad name!"), [1]);

        var listed = await ToListAsync(_provider.ListAsync("acc", default));

        listed.Select(o => o.Key).Should().Equal("acc/ws/attachments/o9");
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("acc/../../outside")]
    [InlineData("/etc")]
    [InlineData("acc//ws")]
    public async Task List_RejectsTraversalAndMalformedPrefixes(string prefix)
    {
        var act = async () => await ToListAsync(_provider.ListAsync(prefix, default));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static async Task<List<StorageObjectInfo>> ToListAsync(IAsyncEnumerable<StorageObjectInfo> source)
    {
        var list = new List<StorageObjectInfo>();
        await foreach (var item in source)
        {
            list.Add(item);
        }

        return list;
    }
}
