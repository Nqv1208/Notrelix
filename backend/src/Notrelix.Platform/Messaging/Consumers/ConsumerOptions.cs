namespace Notrelix.Platform.Messaging.Consumers;

public sealed class ConsumerOptions
{
    public int ConcurrencyLimit { get; set; } = 1;
    public bool OrderingRequired { get; set; }
    public int PoisonThreshold { get; set; } = 10;
    public bool Enabled { get; set; } = true;
}
