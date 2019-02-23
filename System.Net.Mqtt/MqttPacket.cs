namespace System.Net.Mqtt
{
    public abstract class MqttPacket
    {
        public abstract Memory<byte> GetBytes();

        /// <summary>
        /// Tries to write binary representation of the <see cref="MqttPacket" /> to the <paramref name="buffer" /> buffer
        /// </summary>
        /// <param name="buffer">Memory buffer to be written to</param>
        /// <param name="size">Actual size of the data written, or required size if supplied buffer capacity is insufficient</param>
        /// <returns>
        /// <remarks>
        /// Implementation should return <value>true</value> and actual data size written to the buffer,
        /// or return <value>false</value> and required buffer size, if operation cannot be performed due to insufficient buffer
        /// size (callers should adjust buffer size accordingly and try again).
        /// </remarks>
        /// Returns <value>true</value> if data was written  successfully, otherwise <value>false</value> if
        /// <paramref name="buffer" /> size is insufficient.
        /// </returns>
        public abstract bool TryWrite(in Memory<byte> buffer, out int size);
    }
}