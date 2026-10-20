namespace Net.Mqtt;

public interface IMqttPacketWriter
{
    /// <summary>
    /// Writes binary representation of the packet to the <seealso cref="IBufferWriter{T}" /> writer.
    /// </summary>
    /// <param name="writer">Buffer writer instance.</param>
    /// <param name="bytesWritten">Number of bytes actually written to the writer's buffer.</param>
    void Write(IBufferWriter<byte> writer, out int bytesWritten);
}