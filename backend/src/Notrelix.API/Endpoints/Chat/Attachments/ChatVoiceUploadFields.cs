namespace Notrelix.API.Endpoints.Chat.Attachments;

/// <summary>The voice facts of a message file upload (UC-CHT-I04), read from small multipart fields before the file part.</summary>
internal readonly record struct ChatVoiceUpload(bool Voice, int? DurationHintMs, int[]? Waveform);

/// <summary>
/// The optional voice fields of the upload: <c>voice</c> ("true" or "false"), <c>durationMs</c> (a hint, clamped by the application) and
/// <c>waveform</c> (1 to 64 comma-separated peaks, each 0 to 255). Malformed values are 400; absent fields mean a plain file.
/// </summary>
internal static class ChatVoiceUploadFields
{
    public const string VoiceField = "voice";
    public const string DurationField = "durationMs";
    public const string WaveformField = "waveform";

    /// <summary>The waveform peak ceiling of the attachment contract (the Domain FileMetadata limit, kept out of the endpoint layer).</summary>
    private const int MaxWaveformPeaks = 64;

    public static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.Ordinal) { VoiceField, DurationField, WaveformField };

    public static bool TryParse(IReadOnlyDictionary<string, string>? fields, out ChatVoiceUpload upload, out string? error)
    {
        upload = default;
        error = null;
        fields ??= new Dictionary<string, string>();

        var voice = false;
        if (fields.TryGetValue(VoiceField, out var voiceText))
        {
            if (voiceText.Trim() is not ("true" or "false"))
            {
                error = "The voice field is true or false.";
                return false;
            }

            voice = voiceText.Trim() == "true";
        }

        int? duration = null;
        if (fields.TryGetValue(DurationField, out var durationText) && durationText.Trim().Length > 0)
        {
            if (!int.TryParse(durationText.Trim(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var ms) || ms < 0)
            {
                error = "The durationMs field is a whole number of milliseconds.";
                return false;
            }

            duration = ms;
        }

        int[]? waveform = null;
        if (fields.TryGetValue(WaveformField, out var waveformText) && waveformText.Trim().Length > 0)
        {
            var parts = waveformText.Trim().Split(',');
            if (parts.Length > MaxWaveformPeaks)
            {
                error = $"The waveform has at most {MaxWaveformPeaks} peaks.";
                return false;
            }

            waveform = new int[parts.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var peak) || peak is < 0 or > 255)
                {
                    error = "Each waveform peak is a whole number from 0 to 255.";
                    return false;
                }

                waveform[i] = peak;
            }
        }

        upload = new ChatVoiceUpload(voice, duration, waveform);
        return true;
    }
}
