using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Notrelix.API.Extensions;

namespace Notrelix.API.Uploads;

/// <summary>
/// Reads the one <c>file</c> part of a multipart/form-data upload as a forward-only stream, so a
/// file is never buffered in memory or in a temporary file. Shared by every streamed upload
/// endpoint; each caller supplies its own size cap and error codes.
/// </summary>
internal static class MultipartUpload
{
    /// <summary>Allowance for multipart framing (boundaries, part headers) on top of the file cap.</summary>
    public const long MultipartOverheadBytes = 1024 * 1024;

    public const string FilePartName = "file";

    /// <summary>The open file part, or the problem response to return when the request is not a usable upload.</summary>
    internal readonly record struct FilePart(Stream? Body, string FileName, IResult? Failure);

    public static async Task<FilePart> ReadFilePartAsync(
        HttpContext httpContext,
        long maxFileBytes,
        string unsupportedMediaTypeCode,
        string payloadTooLargeCode,
        CancellationToken cancellationToken)
    {
        var request = httpContext.Request;
        var maxRequestBytes = maxFileBytes + MultipartOverheadBytes;

        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaType)
            || !mediaType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value))
        {
            return Fail(Problem(StatusCodes.Status415UnsupportedMediaType, unsupportedMediaTypeCode,
                "Unsupported media type", "The upload must be multipart/form-data with a boundary."));
        }

        // Cheap early rejection from the declared length; the real cap is enforced while streaming.
        if (request.ContentLength > maxRequestBytes)
        {
            return Fail(Problem(StatusCodes.Status413PayloadTooLarge, payloadTooLargeCode,
                "Payload too large", $"The upload exceeds the maximum allowed size of {maxFileBytes} bytes."));
        }

        // Hard transport limit, raised for this route only (null/read-only on hosts without the feature).
        var sizeFeature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (sizeFeature is { IsReadOnly: false })
        {
            sizeFeature.MaxRequestBodySize = maxRequestBytes;
        }

        var reader = new MultipartReader(HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value!, request.Body)
        {
            HeadersLengthLimit = 16 * 1024,
        };

        MultipartSection? section;
        try
        {
            do
            {
                section = await reader.ReadNextSectionAsync(cancellationToken);
            }
            while (section is not null && !IsFilePart(section, out _));
        }
        catch (Exception ex) when (IsMalformedBody(ex))
        {
            return Fail(EndpointExtensions.InvalidInput("The multipart body is malformed."));
        }

        if (section is null || !IsFilePart(section, out var fileName))
        {
            return Fail(EndpointExtensions.InvalidInput("A multipart part named 'file' with a file name is required."));
        }

        return new FilePart(new MalformedBodyAsInvalidDataStream(section.Body), fileName, null);
    }

    private static FilePart Fail(IResult failure) => new(null, string.Empty, failure);

    /// <summary>
    /// A multipart body that ends before its closing boundary surfaces as a plain
    /// <see cref="IOException"/> from the reader; transport failures (including the Kestrel body
    /// limit) are <see cref="BadHttpRequestException"/> and keep their own mapping.
    /// </summary>
    internal static bool IsMalformedBody(Exception ex) =>
        ex is InvalidDataException || (ex is IOException && ex is not BadHttpRequestException);

    /// <summary>
    /// Forward-only wrapper over the file part that reports a truncated multipart body as
    /// <see cref="InvalidDataException"/> (400). Only reads of the request body are translated;
    /// failures of the storage write itself are not touched and stay server errors.
    /// </summary>
    internal sealed class MalformedBodyAsInvalidDataStream(Stream inner) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count) => Translate(() => inner.Read(buffer, offset, count));

        public override int Read(Span<byte> buffer)
        {
            try
            {
                return inner.Read(buffer);
            }
            catch (Exception ex) when (IsMalformedBody(ex) && ex is not InvalidDataException)
            {
                throw new InvalidDataException("The multipart body ended unexpectedly.", ex);
            }
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            await ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            try
            {
                return await inner.ReadAsync(buffer, cancellationToken);
            }
            catch (Exception ex) when (IsMalformedBody(ex) && ex is not InvalidDataException)
            {
                throw new InvalidDataException("The multipart body ended unexpectedly.", ex);
            }
        }

        private static int Translate(Func<int> read)
        {
            try
            {
                return read();
            }
            catch (Exception ex) when (IsMalformedBody(ex) && ex is not InvalidDataException)
            {
                throw new InvalidDataException("The multipart body ended unexpectedly.", ex);
            }
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    internal static bool IsFilePart(MultipartSection section, out string fileName)
    {
        fileName = string.Empty;
        if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition)
            || !disposition.DispositionType.Equals("form-data", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(HeaderUtilities.RemoveQuotes(disposition.Name).Value, FilePartName, StringComparison.Ordinal))
        {
            return false;
        }

        var raw = disposition.FileNameStar.HasValue ? disposition.FileNameStar : disposition.FileName;
        var name = HeaderUtilities.RemoveQuotes(raw).Value;
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        fileName = name;
        return true;
    }

    private static IResult Problem(int status, string errorCode, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Type = $"https://docs.notrelix.com/problems/{errorCode.Replace('.', '-')}",
            Title = title,
            Status = status,
            Detail = detail,
        };
        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? "unknown";
        return TypedResults.Problem(problem);
    }
}
