using System.Buffers.Binary;
using System.Text;

namespace Notrelix.Application.Tests.Features.Collaboration.Attachments;

/// <summary>Builds the leading bytes of audio containers for the header reader and the classifier tests. Only the fields under test are real.</summary>
internal static class AudioHeaderBuilder
{
    // ── EBML (WebM) ────────────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>An EBML element: the ID bytes, a one-byte-or-more size, then the payload.</summary>
    public static byte[] Ebml(byte[] id, byte[] payload)
    {
        var size = Vint(payload.Length);
        return [.. id, .. size, .. payload];
    }

    private static byte[] Vint(long value)
    {
        // Eight-byte size, marker in the first byte, always valid.
        var bytes = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, (ulong)value | (1UL << 56));
        return bytes;
    }

    /// <summary>An EBML size of unknown length (all value bits set), as a live recording's Segment carries.</summary>
    public static byte[] UnknownSize() => [0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];

    public static byte[] Uint(byte[] id, ulong value)
    {
        var payload = new List<byte>();
        for (var shift = 56; shift >= 0; shift -= 8)
        {
            var b = (byte)(value >> shift);
            if (payload.Count > 0 || b != 0 || shift == 0)
                payload.Add(b);
        }

        return Ebml(id, payload.ToArray());
    }

    public static byte[] Float(byte[] id, double value)
    {
        var payload = new byte[8];
        BinaryPrimitives.WriteDoubleBigEndian(payload, value);
        return Ebml(id, payload);
    }

    public static byte[] Id(params byte[] bytes) => bytes;

    public static readonly byte[] EbmlHeaderId = [0x1A, 0x45, 0xDF, 0xA3];
    public static readonly byte[] SegmentId = [0x18, 0x53, 0x80, 0x67];
    public static readonly byte[] InfoId = [0x15, 0x49, 0xA9, 0x66];
    public static readonly byte[] TracksId = [0x16, 0x54, 0xAE, 0x6B];
    public static readonly byte[] TrackEntryId = [0xAE];
    public static readonly byte[] TrackTypeId = [0x83];
    public static readonly byte[] TimecodeScaleId = [0x2A, 0xD7, 0xB1];
    public static readonly byte[] DurationId = [0x44, 0x89];
    public static readonly byte[] ClusterId = [0x1F, 0x43, 0xB6, 0x75];

    /// <summary>A WebM header: EBML header, an unknown-size Segment with an Info (scale, duration) and one track per entry (1 video, 2 audio).</summary>
    public static byte[] WebM(double? durationMs, params int[] trackTypes)
    {
        var ebml = Ebml(EbmlHeaderId, Encoding.ASCII.GetBytes("\x42\x82\x84webm"));
        var info = new List<byte>();
        info.AddRange(Uint(TimecodeScaleId, 1_000_000));
        if (durationMs is { } d)
            info.AddRange(Float(DurationId, d));
        var infoBytes = Ebml(InfoId, info.ToArray());

        var tracks = new List<byte>();
        foreach (var type in trackTypes)
            tracks.AddRange(Ebml(TrackEntryId, Uint(TrackTypeId, (ulong)type)));
        var tracksBytes = Ebml(TracksId, tracks.ToArray());

        return [.. ebml, .. SegmentId, .. UnknownSize(), .. infoBytes, .. tracksBytes];
    }

    // ── ISO base media (MP4, M4A) ──────────────────────────────────────────────────────────────────────────────────────────

    public static byte[] Box(string type, params byte[][] children) => Box(type, Concat(children));

    public static byte[] Box(string type, byte[] payload)
    {
        var size = 8 + payload.Length;
        var header = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(header, (uint)size);
        Encoding.ASCII.GetBytes(type).CopyTo(header, 4);
        return [.. header, .. payload];
    }

    public static byte[] Ftyp(string brand) => Box("ftyp", [.. Encoding.ASCII.GetBytes(brand), 0, 0, 0, 0]);

    /// <summary>An mvhd box, version 0 (32-bit fields) or 1 (64-bit duration).</summary>
    public static byte[] Mvhd(uint timescale, ulong duration, int version = 0)
    {
        var payload = new List<byte> { (byte)version, 0, 0, 0 };
        if (version == 1)
        {
            payload.AddRange(new byte[16]); // creation and modification time, 64-bit each
            payload.AddRange(U32(timescale));
            payload.AddRange(U64(duration));
        }
        else
        {
            payload.AddRange(new byte[8]);
            payload.AddRange(U32(timescale));
            payload.AddRange(U32((uint)duration));
        }

        payload.AddRange(new byte[80]); // rate, volume, matrix and the rest, unread here
        return Box("mvhd", payload.ToArray());
    }

    /// <summary>A hdlr box whose handler type is 'soun' or 'vide'.</summary>
    public static byte[] Hdlr(string handler)
    {
        var payload = new List<byte>(new byte[8]); // version, flags, pre-defined
        payload.AddRange(Encoding.ASCII.GetBytes(handler));
        payload.AddRange(new byte[13]);
        return Box("hdlr", payload.ToArray());
    }

    public static byte[] Trak(string handler) => Box("trak", Box("mdia", Hdlr(handler)));

    private static byte[] U32(uint v) { var b = new byte[4]; BinaryPrimitives.WriteUInt32BigEndian(b, v); return b; }
    private static byte[] U64(ulong v) { var b = new byte[8]; BinaryPrimitives.WriteUInt64BigEndian(b, v); return b; }

    // ── WAV (RIFF) ─────────────────────────────────────────────────────────────────────────────────────────────────────────

    public static byte[] Wav(uint byteRate, uint dataSize)
    {
        var fmt = new byte[16];
        BinaryPrimitives.WriteUInt16LittleEndian(fmt.AsSpan(0), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(fmt.AsSpan(2), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(fmt.AsSpan(4), 16_000);
        BinaryPrimitives.WriteUInt32LittleEndian(fmt.AsSpan(8), byteRate);
        BinaryPrimitives.WriteUInt16LittleEndian(fmt.AsSpan(12), 2);
        BinaryPrimitives.WriteUInt16LittleEndian(fmt.AsSpan(14), 16);

        var bytes = new List<byte>();
        bytes.AddRange("RIFF"u8.ToArray());
        bytes.AddRange(U32LE(4 + 8 + 16 + 8 + dataSize));
        bytes.AddRange("WAVE"u8.ToArray());
        bytes.AddRange("fmt "u8.ToArray());
        bytes.AddRange(U32LE(16));
        bytes.AddRange(fmt);
        bytes.AddRange("data"u8.ToArray());
        bytes.AddRange(U32LE(dataSize));
        return bytes.ToArray();
    }

    private static byte[] U32LE(uint v) { var b = new byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(b, v); return b; }

    // ── MPEG audio ─────────────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>An ID3v2 tag of the given payload size (synchsafe), so the first frame follows it.</summary>
    public static byte[] Id3(int payloadSize)
    {
        var header = new byte[10];
        header[0] = (byte)'I';
        header[1] = (byte)'D';
        header[2] = (byte)'3';
        header[3] = 4;
        header[6] = (byte)((payloadSize >> 21) & 0x7F);
        header[7] = (byte)((payloadSize >> 14) & 0x7F);
        header[8] = (byte)((payloadSize >> 7) & 0x7F);
        header[9] = (byte)(payloadSize & 0x7F);
        return [.. header, .. new byte[payloadSize]];
    }

    /// <summary>
    /// The first MPEG-1 Layer III frame header (44.1 kHz, 128 kbit/s, stereo), then a Xing frame count. <paramref name="mono"/> changes the
    /// channel mode and so where the Xing tag sits.
    /// </summary>
    public static byte[] MpegXing(uint frames, bool mono = false, bool withXing = true, int sampleRateIndex = 0)
    {
        // FF FB: sync, MPEG-1, Layer III, no CRC. Then bitrate index 9 (128 kbit/s) and the sample rate index. Then the channel mode.
        var header = new byte[] { 0xFF, 0xFB, (byte)((9 << 4) | (sampleRateIndex << 2)), (byte)(mono ? 0xC0 : 0x00) };
        var side = new byte[mono ? 17 : 32];
        var frame = new List<byte>();
        frame.AddRange(header);
        frame.AddRange(side);
        if (withXing)
        {
            frame.AddRange("Xing"u8.ToArray());
            frame.AddRange(U32(1)); // flags: frames present
            frame.AddRange(U32(frames));
        }
        else
        {
            frame.AddRange(new byte[12]);
        }

        frame.AddRange(new byte[64]);
        return frame.ToArray();
    }

    /// <summary>A header that is only an Ogg page marker (the duration of Ogg is not in the leading bytes).</summary>
    public static byte[] Ogg() => [.. "OggS"u8.ToArray(), 0, 2, .. new byte[20]];

    public static byte[] Concat(params byte[][] parts)
    {
        var list = new List<byte>();
        foreach (var part in parts)
            list.AddRange(part);
        return list.ToArray();
    }
}
