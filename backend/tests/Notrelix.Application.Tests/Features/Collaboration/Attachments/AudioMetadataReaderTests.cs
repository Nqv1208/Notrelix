using Notrelix.Application.Features.Collaboration.Attachments.Services;
using static Notrelix.Application.Tests.Features.Collaboration.Attachments.AudioHeaderBuilder;

namespace Notrelix.Application.Tests.Features.Collaboration.Attachments;

/// <summary>
/// The duration and track reader of audio uploads (UC-CHT-I04): read from the leading bytes only, advisory, null for anything it cannot
/// read. Never throws on a truncated or garbage header.
/// </summary>
public class AudioMetadataReaderTests
{
    private const string WebM = "audio/webm";
    private const string Mp4 = "audio/mp4";
    private const string Wav = "audio/wav";
    private const string Mpeg = "audio/mpeg";
    private const string Ogg = "audio/ogg";

    [Fact]
    public void A_webm_reads_the_duration_from_the_info_element_with_its_timecode_scale()
    {
        var header = WebM(durationMs: 2_500, trackTypes: 2);

        AudioMetadataReader.TryReadDurationMs(header, WebM).Should().Be(2_500);
    }

    [Fact]
    public void A_webm_without_a_duration_element_has_no_duration_so_the_client_hint_applies()
    {
        // Browser recorders write no Duration: the header carries the track only.
        var header = WebM(durationMs: null, trackTypes: 2);

        AudioMetadataReader.TryReadDurationMs(header, WebM).Should().BeNull();
    }

    [Fact]
    public void A_webm_duration_cut_by_the_end_of_the_window_is_not_read()
    {
        var full = WebM(durationMs: 2_500, trackTypes: 2);
        // Keep the header up to the middle of the Duration float: the value must not be read from a partial element.
        var at = full.AsSpan().IndexOf(DurationId) + 6;
        var cut = full[..at];

        AudioMetadataReader.TryReadDurationMs(cut, WebM).Should().BeNull();
    }

    [Fact]
    public void A_webm_with_a_zero_or_negative_duration_is_not_a_duration()
    {
        AudioMetadataReader.TryReadDurationMs(WebM(durationMs: 0, trackTypes: 2), WebM).Should().BeNull();
        AudioMetadataReader.TryReadDurationMs(WebM(durationMs: -5, trackTypes: 2), WebM).Should().BeNull();
    }

    [Fact]
    public void An_mp4_version_0_movie_header_gives_its_duration_in_milliseconds()
    {
        // Timescale 1000 units per second, duration 4321 units: 4321 ms.
        var header = Concat(Ftyp("M4A "), Box("moov", Mvhd(timescale: 1000, duration: 4321, version: 0)));

        AudioMetadataReader.TryReadDurationMs(header, Mp4).Should().Be(4321);
    }

    [Fact]
    public void An_mp4_version_1_movie_header_reads_a_64_bit_duration()
    {
        // Timescale 48000 units per second, duration 240000 units: 5000 ms.
        var header = Concat(Ftyp("M4A "), Box("moov", Mvhd(timescale: 48_000, duration: 240_000, version: 1)));

        AudioMetadataReader.TryReadDurationMs(header, Mp4).Should().Be(5_000);
    }

    [Fact]
    public void An_mp4_whose_movie_header_is_after_the_window_has_no_duration()
    {
        // A large media box comes first (no faststart): the movie header is not in the leading bytes.
        var header = Concat(Ftyp("M4A "), Box("mdat", new byte[200_000]));

        AudioMetadataReader.TryReadDurationMs(header, Mp4).Should().BeNull();
    }

    [Fact]
    public void A_wav_reads_its_duration_from_the_byte_rate_and_the_data_size()
    {
        // 16000 bytes per second, 32000 data bytes: 2000 ms.
        var header = Wav(byteRate: 16_000, dataSize: 32_000);

        AudioMetadataReader.TryReadDurationMs(header, Wav).Should().Be(2_000);
    }

    [Fact]
    public void A_wav_with_a_zero_byte_rate_has_no_duration()
    {
        AudioMetadataReader.TryReadDurationMs(Wav(byteRate: 0, dataSize: 32_000), Wav).Should().BeNull();
    }

    [Fact]
    public void An_mpeg_file_reads_its_frame_count_from_the_xing_header_after_an_id3_tag()
    {
        // 100 frames of 1152 samples at 44.1 kHz: 2612 ms (115200000 / 44100 rounded down).
        var header = Concat(Id3(payloadSize: 200), MpegXing(frames: 100));

        AudioMetadataReader.TryReadDurationMs(header, Mpeg).Should().Be(2_612);
    }

    [Fact]
    public void A_mono_mpeg_file_puts_its_xing_header_after_the_shorter_side_information()
    {
        AudioMetadataReader.TryReadDurationMs(MpegXing(frames: 100, mono: true), Mpeg).Should().Be(2_612);
    }

    [Fact]
    public void An_mpeg_frame_without_a_xing_header_has_no_duration()
    {
        AudioMetadataReader.TryReadDurationMs(MpegXing(frames: 100, withXing: false), Mpeg).Should().BeNull();
    }

    [Fact]
    public void Ogg_has_no_duration_in_its_leading_bytes()
    {
        AudioMetadataReader.TryReadDurationMs(Ogg(), Ogg).Should().BeNull();
    }

    [Fact]
    public void An_unknown_content_type_has_no_duration()
    {
        AudioMetadataReader.TryReadDurationMs(WebM(2_000, 2), "video/webm").Should().BeNull();
    }

    [Fact]
    public void A_webm_reports_an_audio_only_track_list()
    {
        var tracks = AudioMetadataReader.TryReadTracks(WebM(null, 2), isWebM: true);

        tracks.Should().Be(new ContainerTracks(HasAudio: true, HasVideo: false));
    }

    [Fact]
    public void A_webm_with_a_video_track_reports_video()
    {
        var tracks = AudioMetadataReader.TryReadTracks(WebM(null, 1, 2), isWebM: true);

        tracks.Should().Be(new ContainerTracks(HasAudio: true, HasVideo: true));
    }

    [Fact]
    public void An_mp4_reports_its_handler_types()
    {
        var header = Concat(Ftyp("isom"), Box("moov", Trak("soun")));

        AudioMetadataReader.TryReadTracks(header, isWebM: false).Should().Be(new ContainerTracks(HasAudio: true, HasVideo: false));
    }

    [Fact]
    public void A_header_with_no_track_reports_no_tracks()
    {
        AudioMetadataReader.TryReadTracks(Ftyp("isom"), isWebM: false).Should().BeNull();
        AudioMetadataReader.TryReadTracks([1, 2, 3], isWebM: true).Should().BeNull();
    }

    [Fact]
    public void Garbage_and_every_truncation_of_a_valid_header_read_as_no_duration_and_never_throw()
    {
        var samples = new (byte[] Header, string Type)[]
        {
            (WebM(2_500, 2), WebM),
            (Concat(Ftyp("M4A "), Box("moov", Mvhd(1000, 4321))), Mp4),
            (Wav(16_000, 32_000), Wav),
            (Concat(Id3(64), MpegXing(100)), Mpeg),
        };

        foreach (var (header, type) in samples)
        {
            for (var cut = 0; cut <= header.Length; cut++)
            {
                var prefix = header[..cut];
                var act = () => AudioMetadataReader.TryReadDurationMs(prefix, type);
                act.Should().NotThrow();
            }
        }

        var random = new Random(7);
        for (var i = 0; i < 200; i++)
        {
            var noise = new byte[random.Next(0, 300)];
            random.NextBytes(noise);
            foreach (var type in new[] { WebM, Mp4, Wav, Mpeg })
            {
                var act = () => AudioMetadataReader.TryReadDurationMs(noise, type);
                act.Should().NotThrow();
                var tracks = () => AudioMetadataReader.TryReadTracks(noise, isWebM: type == WebM);
                tracks.Should().NotThrow();
            }
        }
    }

    [Fact]
    public void A_truncated_webm_still_reports_the_tracks_it_contains()
    {
        // The tracks are complete; a cluster that starts after them is cut short, and the walk stops at the cluster.
        var header = Concat(WebM(null, 2), ClusterId, [0x01, 0x00]);

        AudioMetadataReader.TryReadTracks(header, isWebM: true).Should().Be(new ContainerTracks(HasAudio: true, HasVideo: false));
    }
}
