namespace System.Net.Mqtt.Server;

public readonly struct MessageRequest : IEquatable<MessageRequest>
{
    public Message Message { get; }
    public string ClientId { get; }

    public MessageRequest(Message message, string clientId)
    {
        Message = message;
        ClientId = clientId;
    }

    public override bool Equals(object obj)
    {
        return obj is MessageRequest other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Message, ClientId);
    }

    public void Deconstruct(out Message message, out string clientId)
    {
        message = Message;
        clientId = ClientId;
    }

    public static bool operator ==(MessageRequest left, MessageRequest right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MessageRequest left, MessageRequest right)
    {
        return !left.Equals(right);
    }

    public bool Equals(MessageRequest other)
    {
        return other.ClientId == ClientId && EqualityComparer<Message>.Default.Equals(Message, other.Message);
    }
}