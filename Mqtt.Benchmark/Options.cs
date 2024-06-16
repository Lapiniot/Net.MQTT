using Mqtt.Benchmark;
using OOs.CommandLine;

[assembly: Option<string>("Server", "server", 'S')]
[assembly: Option<Protocol>("Protocol", "protocol", 'p')]
[assembly: Option<bool>("ForceHttp2", "force-http-2")]
[assembly: Option<string>("TestProfile", "test", 'T')]
[assembly: Option<int>("NumMessages", "num-messages", 'n')]
[assembly: Option<int>("NumClients", "num-clients", 'c')]
[assembly: Option<int>("NumSubscriptions", "num-subscriptions", 's')]
[assembly: Option<QoSLevel>("QoSLevel", "qos", 'q')]
[assembly: Option<TimeSpan>("TimeoutOverall", "timeout", 't')]
[assembly: Option<TimeSpan>("UpdateInterval", "update-interval", 'i')]
[assembly: Option<bool>("NoProgress", "no-progress")]
[assembly: Option<int?>("MaxConcurrent", "max-concurrent", 'm')]
[assembly: Option<int>("MinPayloadSize", "min-size")]
[assembly: Option<int>("MaxPayloadSize", "max-size")]