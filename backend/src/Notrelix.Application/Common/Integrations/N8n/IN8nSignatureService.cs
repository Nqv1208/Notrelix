namespace Notrelix.Application.Common.Integrations.N8n;

public interface IN8nSignatureService
{
    string CreateSignature(string payload, long timestamp, string secret);

    bool VerifySignature(
        string payload,
        long timestamp,
        string signature,
        string secret,
        TimeSpan tolerance,
        DateTimeOffset? now = null);
}
