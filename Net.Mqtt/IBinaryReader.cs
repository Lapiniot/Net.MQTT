namespace Net.Mqtt;

#pragma warning disable CA1000

public interface IBinaryReader<TSelf> where TSelf : IBinaryReader<TSelf>
{
    static abstract bool TryRead(in ReadOnlySequence<byte> sequence, out TSelf value, out int consumed);
}