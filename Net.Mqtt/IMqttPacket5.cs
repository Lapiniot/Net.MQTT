namespace Net.Mqtt;

public interface IMqttPacket5
{
    /// <summary>
    /// Writes binary representation of the packet to the <seealso cref="IBufferWriter{T}" /> writer.
    /// </summary>
    /// <param name="writer">Buffer writer instance.</param>
    /// <param name="maxAllowedBytes">Maximum size in bytes, allowed to be written.</param>
    /// <returns>Number of bytes actually written to the buffer.</returns>
    public int Write(IBufferWriter<byte> writer, int maxAllowedBytes);
}