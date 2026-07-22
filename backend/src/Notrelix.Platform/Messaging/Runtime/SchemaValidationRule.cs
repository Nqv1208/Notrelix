using Microsoft.Extensions.Logging;
using Notrelix.Platform.Messaging.Contracts;

namespace Notrelix.Platform.Messaging.Runtime;

public sealed class SchemaValidationRule
{
    private readonly ICanonicalizer _canonicalizer;
    private readonly IEventDescriptorProvider _descriptorProvider;
    private readonly ILogger<SchemaValidationRule>? _logger;

    public SchemaValidationRule(
        ICanonicalizer canonicalizer,
        IEventDescriptorProvider descriptorProvider,
        ILogger<SchemaValidationRule>? logger = null)
    {
        _canonicalizer = canonicalizer;
        _descriptorProvider = descriptorProvider;
        _logger = logger;
    }

    public SchemaValidationResult Validate(ReadOnlyMemory<byte> data, string eventName, int version)
    {
        EventDescriptor descriptor;
        try
        {
            descriptor = _descriptorProvider.Get(eventName, version);
        }
        catch (UnknownEventDescriptorException)
        {
            _logger?.LogWarning("No descriptor found for event {Event} v{Version}", eventName, version);
            return SchemaValidationResult.Warn("No schema definition available");
        }

        if (descriptor.Schema is null)
        {
            _logger?.LogDebug("No schema configured for event {Event} v{Version}", eventName, version);
            return SchemaValidationResult.Pass();
        }

        if (descriptor.Deprecated)
        {
            _logger?.LogWarning("Event {Event} v{Version} is deprecated", eventName, version);
            return SchemaValidationResult.Warn($"Event is deprecated since {descriptor.DeprecationDate}");
        }

        var canonical = _canonicalizer.Canonicalize(data);

        return SchemaValidationResult.Pass(canonical);
    }
}

public sealed record SchemaValidationResult
{
    public bool IsValid { get; init; }
    public bool IsWarning { get; init; }
    public string? Message { get; init; }
    public ReadOnlyMemory<byte>? CanonicalData { get; init; }

    public static SchemaValidationResult Pass(ReadOnlyMemory<byte>? canonicalData = null) =>
        new() { IsValid = true, CanonicalData = canonicalData };

    public static SchemaValidationResult Warn(string message) =>
        new() { IsValid = true, IsWarning = true, Message = message };

    public static SchemaValidationResult Fail(string message) =>
        new() { IsValid = false, Message = message };
}
