using FluentAssertions;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Collaboration.Attachments;

namespace Notrelix.Domain.Tests.Collaboration;

/// <summary>The voice facts of an audio file (UC-CHT-I04): a duration within the stored bound and a waveform of 1 to 64 peaks.</summary>
public class VoiceFileMetadataTests
{
    [Fact]
    public void An_audio_file_keeps_its_duration_and_waveform()
    {
        var waveform = new byte[] { 0, 128, 255 };

        var metadata = FileMetadata.Create("voice.webm", 1_000, "audio/webm", storageKey: "k", durationMs: 2_500, waveform: waveform);

        metadata.DurationMs.Should().Be(2_500);
        metadata.Waveform.Should().Equal(waveform);
    }

    [Fact]
    public void A_file_without_voice_facts_has_neither()
    {
        var metadata = FileMetadata.Create("note.pdf", 10, "application/pdf", storageKey: "k");

        metadata.DurationMs.Should().BeNull();
        metadata.Waveform.Should().BeNull();
    }

    [Fact]
    public void A_duration_above_the_stored_bound_is_refused()
    {
        var act = () => FileMetadata.Create("voice.webm", 1, "audio/webm", storageKey: "k", durationMs: FileMetadata.MaxDurationMs + 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void A_waveform_of_more_than_64_peaks_or_none_is_refused()
    {
        var tooMany = () => FileMetadata.Create("voice.webm", 1, "audio/webm", storageKey: "k", waveform: new byte[65]);
        var none = () => FileMetadata.Create("voice.webm", 1, "audio/webm", storageKey: "k", waveform: []);

        tooMany.Should().Throw<ArgumentOutOfRangeException>();
        none.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Two_files_with_the_same_waveform_are_equal_values()
    {
        var a = FileMetadata.Create("voice.webm", 1, "audio/webm", storageKey: "k", durationMs: 1, waveform: [1, 2]);
        var b = FileMetadata.Create("voice.webm", 1, "audio/webm", storageKey: "k", durationMs: 1, waveform: [1, 2]);

        a.Should().Be(b);
    }
}

public class VoiceMessageTextTests
{
    [Fact]
    public void A_voice_message_may_carry_no_text()
    {
        var message = Message.CreateUserMessage(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), string.Empty, "client-1", null, DateTimeOffset.UtcNow, voiceMessage: true);

        message.Content.Should().BeEmpty();
    }

    [Fact]
    public void A_text_message_without_text_is_still_refused()
    {
        var act = () => Message.CreateUserMessage(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), "  ", "client-1", null, DateTimeOffset.UtcNow);

        // The guard refuses blank content before the rule is checked; either way the message is not created.
        act.Should().Throw<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
    }
}
