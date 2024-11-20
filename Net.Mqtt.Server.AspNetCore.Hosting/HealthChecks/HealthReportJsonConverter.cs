using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Net.Mqtt.Server.AspNetCore.Hosting.HealthChecks;

internal sealed class HealthReportJsonConverter : JsonConverter<HealthReport>
{
    public override HealthReport Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, HealthReport value, JsonSerializerOptions options)
    {
        Func<string, string> convertName = options is { PropertyNamingPolicy: { } policy }
            ? name => policy.ConvertName(name)
            : static name => name;

        var healthStatusTypeInfo = options.GetTypeInfo(typeof(HealthStatus));
        var timeSpanTypeInfo = options.GetTypeInfo(typeof(TimeSpan));
        var stringEnumerableTypeInfo = options.GetTypeInfo(typeof(IEnumerable<string>));
        var stringObjectReadOnlyDictionaryTypeInfo = options.GetTypeInfo(typeof(IReadOnlyDictionary<string, object>));

        writer.WriteStartObject();
        writer.WritePropertyName(convertName("Status"));
        JsonSerializer.Serialize(writer, value.Status, healthStatusTypeInfo);
        writer.WritePropertyName(convertName("TotalDuration"));
        JsonSerializer.Serialize(writer, value.TotalDuration, timeSpanTypeInfo);
        writer.WriteStartObject(convertName("Checks"));
        foreach (var (name, entry) in value.Entries)
        {
            writer.WriteStartObject(convertName(name));
            writer.WritePropertyName(convertName("Status"));
            JsonSerializer.Serialize(writer, entry.Status, healthStatusTypeInfo);
            writer.WriteString(convertName("Description"), entry.Description);
            writer.WritePropertyName(convertName("Duration"));
            JsonSerializer.Serialize(writer, entry.Duration, timeSpanTypeInfo);
            writer.WritePropertyName(convertName("Tags"));
            JsonSerializer.Serialize(writer, entry.Tags, stringEnumerableTypeInfo);
            writer.WritePropertyName(convertName("Data"));
            JsonSerializer.Serialize(writer, entry.Data, stringObjectReadOnlyDictionaryTypeInfo);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}