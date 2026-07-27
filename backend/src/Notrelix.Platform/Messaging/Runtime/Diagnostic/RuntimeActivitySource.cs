using System.Diagnostics;

namespace Notrelix.Platform.Messaging.Runtime;

public static class RuntimeActivitySource
{
    public static readonly ActivitySource Instance = new("Notrelix.Platform.Messaging.Runtime", "1.0.0");
}
