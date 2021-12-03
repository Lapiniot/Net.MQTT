using System.Policies;

namespace System.Net.Mqtt.Client;

#pragma warning disable CA1805
public record struct MqttClientBuilder()
{
    private int Version { get; init; } = 3;
    private NetworkTransport Transport { get; init; } = null;
    private ClientSessionStateRepository Repository { get; init; } = null;
    private string ClientId { get; init; } = null;
    private IRetryPolicy Policy { get; init; } = null;

    public MqttClientBuilder WithProtocolV3()
    {
        return this with { Version = 3 };
    }

    public MqttClientBuilder WithProtocolV4()
    {
        return this with { Version = 4 };
    }

    public MqttClientBuilder WithTransport(NetworkTransport transport)
    {
        return this with { Transport = transport };
    }

    public MqttClientBuilder WithClientId(string clientId)
    {
        return this with { ClientId = clientId };
    }

    public MqttClientBuilder WithSessionStateRepository(ClientSessionStateRepository repository)
    {
        return this with { Repository = repository };
    }

    public MqttClientBuilder WithReconnect(IRetryPolicy policy)
    {
        return this with { Policy = policy };
    }

    public MqttClientBuilder WithReconnect(RepeatCondition[] conditions)
    {
        return this with { Policy = new ConditionalRetryPolicy(conditions) };
    }

    public MqttClientBuilder WithReconnect(RepeatCondition condition)
    {
        return this with { Policy = new ConditionalRetryPolicy(new[] { condition }) };
    }

    public MqttClient Build()
    {
        return Version == 3
            ? new MqttClient3(Transport, ClientId ?? Base32.ToBase32String(CorrelationIdGenerator.GetNext()), Repository, Policy)
            : new MqttClient4(Transport, ClientId, Repository, Policy);
    }

    public MqttClient3 BuildV3()
    {
        return new MqttClient3(Transport, ClientId ?? Base32.ToBase32String(CorrelationIdGenerator.GetNext()), Repository, Policy);
    }

    public MqttClient4 BuildV4()
    {
        return new MqttClient4(Transport, ClientId, Repository, Policy);
    }
}