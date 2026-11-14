using System.Buffers.Binary;
using System.Text;

namespace Notrelix.Application.Features.Collaboration.Attachments.Services;

/// <summary>
/// The track kinds a container header declares. Only what the leading bytes show: a track that starts after the window is not seen.
/// </summary>
public readonly record struct ContainerTracks(bool HasAudio, bool HasVideo);

/// <summary>
/// Reads the duration of an audio file from its container header, from the leading bytes only (no decoding, no package). The result is
/// advisory: a truncated, garbage or unsupported header yields null, never an error. Covered: WebM (Segment Info Duration), MP4 and M4A
/// (mvhd, when the movie header is in the window), WAV (fmt byte rate and data size) and MPEG audio with a Xing or Info frame count.
/// Ogg Opus needs the last page, which is not in the leading bytes, so it yields null and the client hint is used instead.
/// </summary>
public static class AudioMetadataReader
{
    private const uint EbmlHeaderId = 0x1A45DFA3;
    private const uint SegmentId = 0x18538067;
    private const uint InfoId = 0x1549A966;
    private const uint TracksId = 0x1654AE6B;
    private const uint TrackEntryId = 0xAE;
    private const uint TrackTypeId = 0x83;
    private const uint TimecodeScaleId = 0x2AD7B1;
    private const uint DurationId = 0x4489;
    private const uint ClusterId = 0x1F43B675;
    private const long DefaultTimecodeScale = 1_000_000;

    /// <summary>
    /// The duration in milliseconds read from the header, or null when it is absent, garbage or not a positive whole number. A value above the
    /// stored bound is returned as read: the voice policy refuses it, and the stored value is clamped by the caller.
    /// </summary>
    public static int? TryReadDurationMs(ReadOnlySpan<byte> header, string contentType)
    {
        var ms = contentType switch
        {
            "audio/webm" => WebMDurationMs(header),
            "audio/mp4" => Mp4DurationMs(header),
            "audio/wav" => WavDurationMs(header),
            "audio/mpeg" => MpegDurationMs(header),
            _ => null,
        };

        return ms is { } value and >= 1 and <= int.MaxValue ? (int)value : null;
    }

    /// <summary>The track kinds of a WebM or MP4 header, or null when the header shows no track.</summary>
    public static ContainerTracks? TryReadTracks(ReadOnlySpan<byte> header, bool isWebM)
    {
        if (isWebM)
        {
            var state = new WebMState();
            WebMWalk(header, 0, header.Length, state, 0);
            return state.HasAudio || state.HasVideo ? new ContainerTracks(state.HasAudio, state.HasVideo) : null;
        }

        var handlers = Mp4HandlerTypes(header);
        if (handlers.Count == 0)
            return null;
        return new ContainerTracks(handlers.Contains("soun"), handlers.Contains("vide"));
    }

    // ── WebM / Matroska (EBML) ─────────────────────────────────────────────────────────────────────────────────────────────

    private sealed class WebMState
    {
        public double? Duration;
        public long TimecodeScale = DefaultTimecodeScale;
        public long? CurrentTrackType;
        public bool HasAudio;
        public bool HasVideo;
    }

    private static long? WebMDurationMs(ReadOnlySpan<byte> h)
    {
        if (h.Length < 4 || ReadId(h, 0, h.Length, out var id) is null || id != EbmlHeaderId)
            return null;

        var state = new WebMState();
        WebMWalk(h, 0, h.Length, state, 0);
        if (state.Duration is not { } duration || duration <= 0 || double.IsNaN(duration) || double.IsInfinity(duration))
            return null;

        return (long)Math.Round(duration * state.TimecodeScale / 1_000_000d);
    }

    private static void WebMWalk(ReadOnlySpan<byte> h, int pos, int end, WebMState state, int depth)
    {
        if (depth > 4)
            return;

        while (pos < end)
        {
            var idLength = ReadId(h, pos, end, out var id);
            if (idLength is null)
                return;
            pos += idLength.Value;

            var size = ReadSize(h, pos, end, out var sizeLength, out var unknown);
            if (sizeLength is null)
                return;
            pos += sizeLength.Value;

            var dataEnd = unknown ? end : (long)pos + size;
            var clippedEnd = (int)Math.Min(dataEnd, end);

            switch (id)
            {
                case InfoId:
                case TracksId:
                    WebMWalk(h, pos, clippedEnd, state, depth + 1);
                    break;
                case TrackEntryId:
                    state.CurrentTrackType = null;
                    WebMWalk(h, pos, clippedEnd, state, depth + 1);
                    if (state.CurrentTrackType is 1)
                        state.HasVideo = true;
                    else if (state.CurrentTrackType is 2)
                        state.HasAudio = true;
                    break;
                case SegmentId:
                    WebMWalk(h, pos, clippedEnd, state, depth + 1);
                    break;
                case ClusterId:
                    // Media data starts here; the header that carries the duration is before it.
                    return;
                case DurationId when dataEnd <= end:
                    state.Duration = size == 4
                        ? BinaryPrimitives.ReadSingleBigEndian(h.Slice(pos, 4))
                        : size == 8 ? BinaryPrimitives.ReadDoubleBigEndian(h.Slice(pos, 8)) : (double?)null;
                    break;
                case TimecodeScaleId when dataEnd <= end && size is >= 1 and <= 8:
                    state.TimecodeScale = (long)ReadUnsigned(h.Slice(pos, (int)size));
                    break;
                case TrackTypeId when dataEnd <= end && size is >= 1 and <= 8:
                    state.CurrentTrackType = (long)ReadUnsigned(h.Slice(pos, (int)size));
                    break;
            }

            if (unknown && id != SegmentId)
                return;
            if (dataEnd > end)
                return;
            pos = (int)dataEnd;
        }
    }

    /// <summary>The length of an EBML element ID (1 to 4 bytes, the marker bit kept), or null when it is not one.</summary>
    private static int? ReadId(ReadOnlySpan<byte> h, int pos, int end, out uint id)
    {
        id = 0;
        if (pos >= end)
            return null;
        var first = h[pos];
        var length = LeadingZeroBytes(first) + 1;
        if (length > 4 || pos + length > end)
            return null;
        for (var i = 0; i < length; i++)
            id = (id << 8) | h[pos + i];
        return length;
    }

    /// <summary>The value of an EBML data size (1 to 8 bytes, marker removed). All value bits set means unknown.</summary>
    private static long ReadSize(ReadOnlySpan<byte> h, int pos, int end, out int? length, out bool unknown)
    {
        length = null;
        unknown = false;
        if (pos >= end)
            return 0;
        var first = h[pos];
        var count = LeadingZeroBytes(first) + 1;
        if (count > 8 || pos + count > end)
            return 0;
        length = count;

        long value = first & (0xFF >> count);
        var allOnes = value == (0xFF >> count);
        for (var i = 1; i < count; i++)
        {
            value = (value << 8) | h[pos + i];
            allOnes &= h[pos + i] == 0xFF;
        }

        unknown = allOnes;
        return value;
    }

    private static int LeadingZeroBytes(byte first) => first switch
    {
        >= 0x80 => 0,
        >= 0x40 => 1,
        >= 0x20 => 2,
        >= 0x10 => 3,
        >= 0x08 => 4,
        >= 0x04 => 5,
        >= 0x02 => 6,
        >= 0x01 => 7,
        _ => 8,
    };

    private static ulong ReadUnsigned(ReadOnlySpan<byte> bytes)
    {
        ulong value = 0;
        foreach (var b in bytes)
            value = (value << 8) | b;
        return value;
    }

    // ── MP4 / M4A (ISO base media) ─────────────────────────────────────────────────────────────────────────────────────────

    private static readonly HashSet<string> Mp4Containers = ["moov", "trak", "mdia"];

    /// <summary>A box of the header: its four-character type and where its payload sits in the window.</summary>
    private readonly record struct Mp4Box(string Type, int Start, int Length);

    private static long? Mp4DurationMs(ReadOnlySpan<byte> h)
    {
        var boxes = new List<Mp4Box>();
        CollectMp4Boxes(h, 0, h.Length, boxes, 0);
        var movie = boxes.FirstOrDefault(b => b.Type == "mvhd");
        if (movie.Length < 4)
            return null;

        var d = h.Slice(movie.Start, movie.Length);
        if (d[0] == 1 && d.Length >= 32)
        {
            var timescale = BinaryPrimitives.ReadUInt32BigEndian(d.Slice(20, 4));
            var duration = BinaryPrimitives.ReadUInt64BigEndian(d.Slice(24, 8));
            return timescale > 0 ? (long)(duration * 1000UL / timescale) : null;
        }

        if (d[0] == 0 && d.Length >= 20)
        {
            var timescale = BinaryPrimitives.ReadUInt32BigEndian(d.Slice(12, 4));
            var duration = BinaryPrimitives.ReadUInt32BigEndian(d.Slice(16, 4));
            return timescale > 0 ? (long)duration * 1000 / timescale : null;
        }

        return null;
    }

    /// <summary>The handler types ('soun', 'vide', ...) of the tracks whose headers sit in the window.</summary>
    private static List<string> Mp4HandlerTypes(ReadOnlySpan<byte> h)
    {
        var boxes = new List<Mp4Box>();
        CollectMp4Boxes(h, 0, h.Length, boxes, 0);
        var handlers = new List<string>();
        foreach (var box in boxes)
        {
            if (box.Type == "hdlr" && box.Length >= 12)
                handlers.Add(Encoding.ASCII.GetString(h.Slice(box.Start + 8, 4)));
        }

        return handlers;
    }

    /// <summary>Lists the boxes of a window; containers are entered, every other box is listed. A box that runs past the window ends the walk.</summary>
    private static void CollectMp4Boxes(ReadOnlySpan<byte> h, int pos, int end, List<Mp4Box> boxes, int depth)
    {
        if (depth > 4)
            return;

        while (pos + 8 <= end)
        {
            var size = BinaryPrimitives.ReadUInt32BigEndian(h.Slice(pos, 4));
            var type = Encoding.ASCII.GetString(h.Slice(pos + 4, 4));
            var headerLength = 8;
            long boxEnd;
            if (size == 1)
            {
                if (pos + 16 > end)
                    return;
                boxEnd = pos + (long)BinaryPrimitives.ReadUInt64BigEndian(h.Slice(pos + 8, 8));
                headerLength = 16;
            }
            else if (size == 0)
            {
                boxEnd = end;
            }
            else
            {
                boxEnd = pos + (long)size;
            }

            if (boxEnd < pos + headerLength)
                return;

            var dataStart = pos + headerLength;
            var clipped = (int)Math.Min(boxEnd, end);
            if (Mp4Containers.Contains(type))
                CollectMp4Boxes(h, dataStart, clipped, boxes, depth + 1);
            else if (boxEnd <= end)
                boxes.Add(new Mp4Box(type, dataStart, clipped - dataStart));

            if (boxEnd > end)
                return;
            pos = (int)boxEnd;
        }
    }

    // ── WAV (RIFF) ─────────────────────────────────────────────────────────────────────────────────────────────────────────

    private static long? WavDurationMs(ReadOnlySpan<byte> h)
    {
        if (h.Length < 12 || !h.Slice(8, 4).SequenceEqual("WAVE"u8))
            return null;

        long? byteRate = null;
        var pos = 12;
        while (pos + 8 <= h.Length)
        {
            var id = h.Slice(pos, 4);
            var size = BinaryPrimitives.ReadUInt32LittleEndian(h.Slice(pos + 4, 4));
            var data = pos + 8;
            if (id.SequenceEqual("fmt "u8))
            {
                if (data + 12 > h.Length)
                    return null;
                byteRate = BinaryPrimitives.ReadUInt32LittleEndian(h.Slice(data + 8, 4));
            }
            else if (id.SequenceEqual("data"u8))
            {
                if (byteRate is not > 0)
                    return null;
                return (long)size * 1000 / byteRate.Value;
            }

            pos = (int)Math.Min(data + (long)size + (size & 1), int.MaxValue - 16);
        }

        return null;
    }

    // ── MPEG audio (MP3) ───────────────────────────────────────────────────────────────────────────────────────────────────

    private static readonly int[] Mpeg1Rates = [44100, 48000, 32000];
    private static readonly int[] Mpeg2Rates = [22050, 24000, 16000];
    private static readonly int[] Mpeg25Rates = [11025, 12000, 8000];

    private static long? MpegDurationMs(ReadOnlySpan<byte> h)
    {
        var pos = 0;
        if (h.Length >= 10 && h[0] == 'I' && h[1] == 'D' && h[2] == '3')
        {
            var tagSize = (h[6] & 0x7F) << 21 | (h[7] & 0x7F) << 14 | (h[8] & 0x7F) << 7 | (h[9] & 0x7F);
            pos = 10 + tagSize;
        }

        // The first frame sync at or after the tag, within a short scan.
        var limit = Math.Min(h.Length - 4, pos + 4096);
        while (pos <= limit && !(h[pos] == 0xFF && (h[pos + 1] & 0xE0) == 0xE0))
            pos++;
        if (pos > limit)
            return null;

        var b1 = h[pos + 1];
        var b2 = h[pos + 2];
        var b3 = h[pos + 3];
        var version = (b1 >> 3) & 0x3; // 0 = 2.5, 2 = 2, 3 = 1
        var layer = (b1 >> 1) & 0x3;   // 1 = Layer III
        if (version == 1 || layer != 1)
            return null;

        var rateIndex = (b2 >> 2) & 0x3;
        if (rateIndex == 3)
            return null;
        var mono = (b3 >> 6) == 3;
        var isMpeg1 = version == 3;
        var sampleRate = isMpeg1 ? Mpeg1Rates[rateIndex] : version == 2 ? Mpeg2Rates[rateIndex] : Mpeg25Rates[rateIndex];
        var samplesPerFrame = isMpeg1 ? 1152 : 576;

        // The Xing or Info frame count sits after the side information of the first frame.
        var sideInfo = isMpeg1 ? (mono ? 17 : 32) : (mono ? 9 : 17);
        var tag = pos + 4 + sideInfo;
        if (tag + 16 > h.Length)
            return null;
        var isXing = h.Slice(tag, 4).SequenceEqual("Xing"u8) || h.Slice(tag, 4).SequenceEqual("Info"u8);
        if (!isXing)
            return null;
        var flags = BinaryPrimitives.ReadUInt32BigEndian(h.Slice(tag + 4, 4));
        if ((flags & 1) == 0)
            return null;
        var frames = BinaryPrimitives.ReadUInt32BigEndian(h.Slice(tag + 8, 4));
        return (long)frames * samplesPerFrame * 1000 / sampleRate;
    }
}

