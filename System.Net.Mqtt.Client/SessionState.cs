﻿using System.Collections.Generic;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Client
{
    public class SessionState : IDisposable
    {
        private readonly FastPacketIdPool pool;
        private readonly HashSet<ushort> receivedQoS2;
        protected readonly HashQueue<ushort, MqttPacket> ResendQueue;

        public SessionState()
        {
            pool = new FastPacketIdPool();
            receivedQoS2 = new HashSet<ushort>();
            ResendQueue = new HashQueue<ushort, MqttPacket>();
        }

        public ushort Rent()
        {
            return pool.Rent();
        }

        public void Return(ushort id)
        {
            pool.Return(id);
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
            ResendQueue.AddOrUpdate(id, packet, packet);
            return packet;
        }

        public PubRelPacket AddPubRelToResend(ushort id)
        {
            var pubRelPacket = new PubRelPacket(id);
            ResendQueue.AddOrUpdate(id, pubRelPacket, pubRelPacket);
            return pubRelPacket;
        }

        public bool RemoveFromResend(ushort id)
        {
            if(!ResendQueue.TryRemove(id, out _)) return false;
            pool.Return(id);
            return true;
        }

        public IEnumerable<MqttPacket> GetResendPackets()
        {
            return ResendQueue;
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                ResendQueue?.Dispose();
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