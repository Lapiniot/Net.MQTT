namespace Net.Mqtt;

public interface IBinaryReader<TSelf> where TSelf : IBinaryReader<TSelf>
{
    static abstract bool TryRead(in ReadOnlySequence<byte> sequence, out TSelf value, out int consumed);
}