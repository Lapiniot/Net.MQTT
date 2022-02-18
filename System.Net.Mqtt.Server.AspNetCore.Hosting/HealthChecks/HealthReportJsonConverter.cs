using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting.HealthChecks;

internal class HealthReportJsonConverter : JsonConverter<HealthReport>
{
    public override HealthReport Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

    public override void Write(Utf8JsonWriter writer, HealthReport value, JsonSerializerOptions options)
    {
        Func<string, string> convertName = options is { PropertyNamingPolicy: { } policy }
            ? name => policy.ConvertName(name)
            : name => name;
        var converter = (JsonConverter<IReadOnlyDictionary<string, object>>)options.GetConverter(typeof(IReadOnlyDictionary<string, object>));

        writer.WriteStartObject();
        writer.WriteString(convertName("Status"), value.Status.ToString());
        writer.WriteStartObject(convertName("Checks"));
        foreach (var (name, entry) in value.Entries)
        {
            writer.WriteStartObject(convertName(name));
            writer.WriteString(convertName("Status"), entry.Status.ToString());
            writer.WriteString(convertName("Description"), entry.Description);
            writer.WritePropertyName(convertName("Data"));
            converter.Write(writer, entry.Data, options);
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}