namespace Net.Mqtt;

public interface IMqttPacketWriter
{
    /// <summary>
    /// Writes binary representation of the packet to the <seealso cref="IBufferWriter{T}" /> writer.
    /// </summary>
    /// <param name="writer">Buffer writer instance.</param>
    /// <returns>The number of bytes written and the packet type.</returns>
    PacketWriteResult Write(IBufferWriter<byte> writer);
}