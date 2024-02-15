using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Net.Mqtt.Server.AspNetCore.Hosting.HealthChecks;

internal sealed class HealthReportJsonConverter : JsonConverter<HealthReport>
{
    public override HealthReport Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException();

    [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
    public override void Write(Utf8JsonWriter writer, HealthReport value, JsonSerializerOptions options)
    {
        Func<string, string> convertName = options is { PropertyNamingPolicy: { } policy }
            ? name => policy.ConvertName(name)
            : name => name;
        var converter = (JsonConverter<IReadOnlyDictionary<string, object>>)options.GetConverter(typeof(IReadOnlyDictionary<string, object>));

        writer.WriteStartObject();
        writer.WriteString(convertName("Status"), GetString(value.Status));
        writer.WriteStartObject(convertName("Checks"));
        foreach (var (name, entry) in value.Entries)
        {
            writer.WriteStartObject(convertName(name));
            writer.WriteString(convertName("Status"), GetString(entry.Status));
            writer.WriteString(convertName("Description"), entry.Description);
            writer.WritePropertyName(convertName("Data"));
            converter.Write(writer, entry.Data, options);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static string GetString(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => "Healthy",
        HealthStatus.Unhealthy => "Unhealthy",
        HealthStatus.Degraded => "Degraded",
        _ => ThrowInvalidEnumValue()
    };

    [DoesNotReturn]
    private static string ThrowInvalidEnumValue() => throw new ArgumentException("Invalid enum value.");
}