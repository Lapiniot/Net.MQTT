using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Net.Mqtt.Server.AspNetCore.Hosting.HealthChecks;

[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(HealthReport))]
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Serialization,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    Converters = [typeof(HealthReportJsonConverter)])]
[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
    Justification = "Member 'System.Exception.TargetSite.get' is not used because custom HealthReportJsonConverter omits Exception property completely.")]
public partial class HealthChecksJsonContext : JsonSerializerContext { }