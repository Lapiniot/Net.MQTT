using System.Collections.Generic;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Client
{
    public class MqttClientSessionState : IDisposable
    {
        private readonly FastPacketIdPool pool;
        private readonly HashSet<ushort> receivedQoS2;
        private readonly HashQueueCollection<ushort, MqttPacket> resendQueue;

        public MqttClientSessionState()
        {
            pool = new FastPacketIdPool();
            receivedQoS2 = new HashSet<ushort>();
            resendQueue = new HashQueueCollection<ushort, MqttPacket>();
        }

        public ushort Rent()
        {
            return pool.Rent();
        }

        public void Return(ushort id)
        {
            pool.Release(id);
        }

        public bool TryAddQoS2(ushort id)
        {
            return receivedQoS2.Add(id);
        }

        public bool RemoveQoS2(ushort id)
        {
            return receivedQoS2.Remove(id);
        }

        public PublishPacket AddPublishToResend(string topic, Memory<byte> payload, byte qosLevel, bool retain)
        {
            var id = pool.Rent();
            var packet = new PublishPacket(id, qosLevel, topic, payload, retain);
            resendQueue.AddOrUpdate(id, packet, packet);
            return packet;
        }

        public PubRelPacket AddPubRelToResend(ushort id)
        {
            var pubRelPacket = new PubRelPacket(id);
            resendQueue.AddOrUpdate(id, pubRelPacket, pubRelPacket);
            return pubRelPacket;
        }

        public bool RemoveFromResend(ushort id)
        {
            if(!resendQueue.TryRemove(id, out _)) return false;
            pool.Release(id);
            return true;
        }

        public IEnumerable<MqttPacket> GetResendPackets()
        {
            return resendQueue;
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                resendQueue?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}