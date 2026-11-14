using System.Text;
using Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Domain.Collaboration.Attachments;
using static Notrelix.Application.Tests.Features.Collaboration.Attachments.AudioHeaderBuilder;

namespace Notrelix.Application.Tests.Features.Collaboration.Attachments;

/// <summary>The content type of audio uploads comes from the leading bytes (UC-CHT-I04), never from the client's name or declared type.</summary>
public class AudioContentClassifierTests
{
    [Fact]
    public void A_webm_with_only_an_audio_track_is_audio_webm()
    {
        var result = AttachmentContentClassifier.Classify(WebM(null, 2), "voice.webm");

        result.Should().Be(new ClassifiedContent("audio/webm", AttachmentType.Audio));
    }

    [Fact]
    public void A_webm_with_a_video_track_stays_video_webm()
    {
        var result = AttachmentContentClassifier.Classify(WebM(null, 1, 2), "clip.webm");

        result.Should().Be(new ClassifiedContent("video/webm", AttachmentType.Video));
    }

    [Fact]
    public void A_webm_whose_tracks_are_not_in_the_window_keeps_the_video_label_as_before()
    {
        var result = AttachmentContentClassifier.Classify(WebM(null, 2)[..4], "unknown.webm");

        result.Should().Be(new ClassifiedContent("video/webm", AttachmentType.Video));
    }

    [Fact]
    public void An_m4a_file_is_audio_mp4_and_another_mp4_stays_video()
    {
        AttachmentContentClassifier.Classify(Ftyp("M4A "), "voice.m4a").Should().Be(new ClassifiedContent("audio/mp4", AttachmentType.Audio));
        AttachmentContentClassifier.Classify(Ftyp("isom"), "clip.mp4").Should().Be(new ClassifiedContent("video/mp4", AttachmentType.Video));
    }

    [Fact]
    public void A_wave_file_is_audio_wav()
    {
        AttachmentContentClassifier.Classify(Wav(16_000, 100), "note.wav").Should().Be(new ClassifiedContent("audio/wav", AttachmentType.Audio));
    }

    [Fact]
    public void An_ogg_page_is_audio_ogg()
    {
        AttachmentContentClassifier.Classify(Ogg(), "voice.ogg").Should().Be(new ClassifiedContent("audio/ogg", AttachmentType.Audio));
    }

    [Fact]
    public void An_id3_tagged_or_frame_synced_mp3_is_audio_mpeg()
    {
        AttachmentContentClassifier.Classify(Id3(16), "song.mp3").Should().Be(new ClassifiedContent("audio/mpeg", AttachmentType.Audio));
        AttachmentContentClassifier.Classify(MpegXing(10), "song.bin").Should().Be(new ClassifiedContent("audio/mpeg", AttachmentType.Audio));
    }

    [Fact]
    public void A_jpeg_is_still_an_image_not_an_mpeg_frame()
    {
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, (byte)'J', (byte)'F', (byte)'I', (byte)'F', 0, 1, 1, 0, 0, 1, 0, 1, 0, 0 };

        AttachmentContentClassifier.Classify(jpeg, "photo.jpg").Should().Be(new ClassifiedContent("image/jpeg", AttachmentType.Image));
    }

    [Fact]
    public void Plain_text_is_still_text()
    {
        var text = Encoding.UTF8.GetBytes("hello world, this is a note");

        AttachmentContentClassifier.Classify(text, "note.txt").Should().Be(new ClassifiedContent("text/plain", AttachmentType.Document));
    }
}
