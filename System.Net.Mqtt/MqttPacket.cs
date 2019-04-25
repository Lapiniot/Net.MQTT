namespace System.Net.Mqtt
{
    public abstract class MqttPacket
    {
        /// <summary>
        /// Returns size of binary representation in bytes
        /// </summary>
        /// <param name="remainingLength">Gives effective size of the packet specific remainingLength data</param>
        /// <returns>Total size in bytes needed to represent entire packet.</returns>
        public abstract int GetSize(out int remainingLength);

        /// <summary>
        /// Writes binary representation of the packet to the <seealso cref="Span{T}" /> buffer
        /// </summary>
        /// <param name="span"><seealso cref="Span{T}" /> to be written to</param>
        /// <param name="remainingLength">Value of the packet's fixed header remaining length field</param>
        /// <remarks>
        /// <paramref name="remainingLength" /> must be calculated by the caller and provided externally.
        /// This requirement is due to the performance considerations: in some cases, repeating packet
        /// size calculations may be costly (if UTF-8 strings length calculation is involved e.g.),
        /// but must be known in advance (to allocate buffer first of all, and to write variable length
        /// to the header then).
        /// So, typical optimized flow is:
        /// 1. Call <see cref="GetSize" /> to get total buffer size needed and remaining length header value separately in one call
        /// 2. Allocate sufficiently sized buffer
        /// 3. Call <see cref="Write" /> passing buffer from step 2 and remaining length value from step 1.
        /// </remarks>
        public abstract void Write(Span<byte> span, int remainingLength);
    }
}