namespace System.Net.Mqtt.Packets
{
    public class PingRespPacket : MqttPacket
    {
        #region Overrides of MqttPacket

        public override Memory<byte> GetBytes()
        {
            throw new NotImplementedException();
        }

        public override bool TryWrite(in Memory<byte> buffer, out int size)
        {
            size = 2;
            if(size > buffer.Length) return false;
            var span = buffer.Span;
            span[0] = 0b1101_0000;
            span[1] = 0;
            return true;
        }

        #endregion
    }
}