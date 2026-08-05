using System.Text.Json;
using System.Text.Json.Serialization;
using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Canonical JSON settings for idempotency replay serialization (spec 3.5/3.7).
///
/// Responses are stored as JSON and reconstructed on replay. The web defaults
/// mirror the API HTTP serializer (camelCase, string enums), and the Result
/// converter restores the <see cref="Result"/>/<see cref="Result{T}"/> envelopes
/// whose internal constructors are invisible to the default serializer — without
/// it, replay of any Result-wrapped response would throw.
/// </summary>
public static class IdempotencyJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new ResultJsonConverterFactory());
        return options;
    }
}

/// <summary>
/// Round-trips <see cref="Result"/> and <see cref="Result{T}"/> through their
/// internal constructors (same assembly). Typed errors are preserved when present.
/// </summary>
internal sealed class ResultJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(Result)
            || (typeToConvert.IsGenericType
                && typeToConvert.GetGenericTypeDefinition() == typeof(Result<>));
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(Result))
        {
            return new ResultJsonConverter();
        }

        var dataType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(ResultOfJsonConverter<>).MakeGenericType(dataType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed record ResultPayload(
        bool Succeeded,
        string[] Errors,
        ApplicationError[] TypedErrors);

    private sealed record ResultPayload<T>(
        bool Succeeded,
        string[] Errors,
        ApplicationError[] TypedErrors,
        T? Data);

    private sealed class ResultJsonConverter : JsonConverter<Result>
    {
        public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var payload = document.RootElement.Deserialize<ResultPayload>(options)
                ?? throw new JsonException("Invalid idempotency replay payload for Result.");

            var typedErrors = payload.TypedErrors ?? [];
            return typedErrors.Length > 0
                ? new Result(payload.Succeeded, typedErrors)
                : new Result(payload.Succeeded, payload.Errors ?? []);
        }

        public override void Write(Utf8JsonWriter writer, Result value, JsonSerializerOptions options)
        {
            var payload = new ResultPayload(
                value.Succeeded,
                value.Errors,
                value.TypedErrors.ToArray());
            JsonSerializer.Serialize(writer, payload, options);
        }
    }

    private sealed class ResultOfJsonConverter<T> : JsonConverter<Result<T>>
    {
        public override Result<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var payload = document.RootElement.Deserialize<ResultPayload<T>>(options)
                ?? throw new JsonException($"Invalid idempotency replay payload for Result<{typeof(T).Name}>.");

            var typedErrors = payload.TypedErrors ?? [];
            return typedErrors.Length > 0
                ? new Result<T>(payload.Succeeded, payload.Data, typedErrors)
                : new Result<T>(payload.Succeeded, payload.Data, payload.Errors ?? []);
        }

        public override void Write(Utf8JsonWriter writer, Result<T> value, JsonSerializerOptions options)
        {
            var payload = new ResultPayload<T>(
                value.Succeeded,
                value.Errors,
                value.TypedErrors.ToArray(),
                value.Data);
            JsonSerializer.Serialize(writer, payload, options);
        }
    }
}
