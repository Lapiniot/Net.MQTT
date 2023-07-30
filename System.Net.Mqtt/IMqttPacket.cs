namespace System.Net.Mqtt;

public interface IMqttPacket
{
    /// <summary>
    /// Writes binary representation of the packet to the <seealso cref="IBufferWriter{T}" /> writer.
    /// </summary>
    /// <param name="writer">Buffer writer instance.</param>
    /// <param name="buffer">Underlaying buffer provided by the <paramref name="writer" /> for this write operation.</param>
    /// <returns>Number of bytes actually written to the <paramref name="buffer" />.</returns>
    public int Write(IBufferWriter<byte> writer, out Span<byte> buffer);
}