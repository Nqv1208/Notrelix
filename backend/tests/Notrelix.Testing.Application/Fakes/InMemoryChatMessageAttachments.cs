using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.Collaboration.Public.Attachments;

namespace Notrelix.Testing.Application.Fakes;

/// <summary>
/// An in-memory stand-in for the Collaboration boundary of the files on a Chat message. It keeps the rows and the bytes in lists and
/// applies no file policy: tests of the policy run the real flow, and tests of Chat only need files to exist, be listed, open and go.
/// </summary>
public sealed class InMemoryChatMessageAttachments : IChatMessageAttachments
{
    private readonly List<(ChatMessageAttachmentFact Fact, byte[] Bytes)> _files = [];
    private readonly Guid _uploader;

    public InMemoryChatMessageAttachments(Guid? uploader = null)
    {
        _uploader = uploader ?? Guid.NewGuid();
    }

    /// <summary>The files currently held, oldest first.</summary>
    public IReadOnlyList<ChatMessageAttachmentFact> Files => _files.Select(f => f.Fact).ToList();

    public int UploadCalls { get; private set; }

    /// <summary>How many list queries ran, so a test can prove a page of messages costs one.</summary>
    public int ListCalls { get; private set; }

    /// <summary>Puts a file on a message without going through an upload, for arranging a test.</summary>
    public ChatMessageAttachmentFact Seed(Guid messageId, string fileName = "report.pdf", long size = 12, string contentType = "application/pdf", Guid? uploadedBy = null)
    {
        var fact = new ChatMessageAttachmentFact(Guid.NewGuid(), messageId, fileName, size, contentType, uploadedBy ?? _uploader, DateTimeOffset.UtcNow);
        _files.Add((fact, new byte[size]));
        return fact;
    }

    public async Task<ChatMessageAttachmentFact> UploadAsync(
        Guid messageId, string fileName, Stream content, ChatMessageAudioUpload? audio, CancellationToken cancellationToken)
    {
        UploadCalls++;
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var fact = new ChatMessageAttachmentFact(
            Guid.NewGuid(), messageId, fileName, buffer.Length, "application/octet-stream", _uploader, DateTimeOffset.UtcNow);
        _files.Add((fact, buffer.ToArray()));
        return fact;
    }

    public Task<IReadOnlyDictionary<Guid, IReadOnlyList<ChatMessageAttachmentFact>>> ListAsync(
        IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken)
    {
        ListCalls++;
        IReadOnlyDictionary<Guid, IReadOnlyList<ChatMessageAttachmentFact>> map = _files
            .Where(f => messageIds.Contains(f.Fact.MessageId))
            .GroupBy(f => f.Fact.MessageId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ChatMessageAttachmentFact>)g.Select(f => f.Fact).ToList());
        return Task.FromResult(map);
    }

    public Task<ChatMessageAttachmentContent> OpenAsync(Guid messageId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var file = _files.FirstOrDefault(f => f.Fact.Id == attachmentId && f.Fact.MessageId == messageId);
        if (file.Fact is null)
            throw new NotFoundException("Attachment", attachmentId);
        return Task.FromResult(new ChatMessageAttachmentContent(new MemoryStream(file.Bytes), file.Fact.FileName, file.Fact.ContentType, file.Fact.SizeBytes));
    }

    public Task<bool> RemoveAsync(Guid messageId, Guid attachmentId, CancellationToken cancellationToken) =>
        Task.FromResult(_files.RemoveAll(f => f.Fact.Id == attachmentId && f.Fact.MessageId == messageId) > 0);

    public Task<int> RemoveAllAsync(Guid messageId, CancellationToken cancellationToken) =>
        Task.FromResult(_files.RemoveAll(f => f.Fact.MessageId == messageId));

    public Task<int> RemoveAllAsync(IReadOnlyCollection<Guid> messageIds, Guid deletedBy, CancellationToken cancellationToken) =>
        Task.FromResult(_files.RemoveAll(f => messageIds.Contains(f.Fact.MessageId)));
}
