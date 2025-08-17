using OOs.CommandLine;

namespace Mqtt.Benchmark;

[Option<string>("Server", "server", 'S',
    Description = "MQTT server to connect to",
    Hint = "uri")]
[Option<Protocol>("Protocol", "protocol", 'p',
    Description = "MQTT protocol version to use, otherwise try to connect using the highest supported version (5, 4, 3)",
    Hint = "3..5")]
[Option<bool>("ForceHttp2", "force-http-2", 'f',
    Description = "Force the use of HTTP/2 for WebSocket connections (WebSocket transport only)")]
[Option<string>("TestProfile", "test", 'T',
    Description = "Test profile to use", Hint = "name")]
[Option<int>("NumMessages", "num-messages", 'n',
    Description = "Number of messages each client will send", Hint = "number")]
[Option<int>("NumClients", "num-clients", 'c',
    Description = "Number of clients to connect to the MQTT server", Hint = "number")]
[Option<int>("NumSubscriptions", "num-subscriptions", 's',
    Description = "Number of subscriptions each client will create ('subscribe_publish_receive' derived profile only)",
    Hint = "number")]
[Option<QoSLevel>("QoSLevel", "qos", 'q',
    Description = "Quality of Service (QoS) level to use for message delivery (default is QoS0)",
    Hint = "qos0|qos1|qos2|0|1|2")]
[Option<TimeSpan>("TimeoutOverall", "timeout", 't',
    Description = "Overall timeout for the benchmark test",
    Hint = "timespan")]
[Option<TimeSpan>("UpdateInterval", "update-interval", 'i',
    Description = "Interval at which progress will be updated in the console",
    Hint = "timespan")]
[Option<bool>("NoProgress", "no-progress", 'x',
    Description = "Hide progress updates in the console")]
[Option<int>("MaxConcurrent", "max-concurrent", 'm',
    Description = "Maximum number of concurrent clients that can be connected at the same time",
    Hint = "number")]
[Option<int>("MinPayloadSize", "min-size",
    Description = "Min size of the randomly-sized payload for each message",
    Hint = "number")]
[Option<int>("MaxPayloadSize", "max-size",
    Description = "Max size of the randomly-sized payload for each message",
    Hint = "number")]
[ArgumentParserGenerationOptions(GenerateSynopsis = true, AddStandardOptions = true)]
internal partial struct Arguments { }