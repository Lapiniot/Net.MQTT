using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Mqtt.MqttTopicHelpers;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public class SessionState : Server.SessionState
    {
        private readonly IPacketIdPool idPool;
        protected internal readonly int ParallelMatchThreshold;
        private readonly ChannelReader<Message> reader;
        private readonly HashSet<ushort> receivedQos2;
        private readonly HashQueue<ushort, MqttPacket> resendQueue;
        private readonly ConcurrentDictionary<string, byte> subscriptions;
        private readonly ChannelWriter<Message> writer;

        public SessionState(bool persistent)
        {
            Persistent = persistent;
            subscriptions = new ConcurrentDictionary<string, byte>(1, 31);
            idPool = new FastPacketIdPool();
            receivedQos2 = new HashSet<ushort>();
            resendQueue = new HashQueue<ushort, MqttPacket>();

            var channel = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
            writer = channel.Writer;
            reader = channel.Reader;

            ParallelMatchThreshold = 16;
        }

        public bool Persistent { get; }

        public bool IsActive { get; set; }

        public Message WillMessage { get; set; }

        public bool TryAddQoS2(ushort packetId)
        {
            return receivedQos2.Add(packetId);
        }

        public bool RemoveQoS2(ushort packetId)
        {
            return receivedQos2.Remove(packetId);
        }

        public PublishPacket AddPublishToResend(string topic, Memory<byte> payload, byte qoSLevel)
        {
            var id = idPool.Rent();
            var packet = new PublishPacket(id, qoSLevel, topic, payload, duplicate: true);
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
            idPool.Return(id);
            return true;
        }

        public IEnumerable<MqttPacket> GetResendPackets()
        {
            return resendQueue;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if(disposing) resendQueue.Dispose();
        }

        #region Subscription state

        public override IDictionary<string, byte> GetSubscriptions()
        {
            return subscriptions;
        }

        public override byte[] Subscribe((string filter, byte qosLevel)[] filters)
        {
            var length = filters.Length;

            var result = new byte[length];

            for(var i = 0; i < length; i++)
            {
                var (filter, qos) = filters[i];

                var value = qos;

                result[i] = IsValidTopic(filter) ? subscriptions.AddOrUpdate(filter, value, (_, __) => value) : (byte)0x80;
            }

            return result;
        }

        public override void Unsubscribe(string[] filters)
        {
            foreach(var filter in filters)
            {
                subscriptions.TryRemove(filter, out _);
            }
        }

        #endregion

        #region Incoming message delivery state

        public override ValueTask EnqueueAsync(Message message)
        {
            // Skip all incoming QoS 0 if session is inactive
            if(!IsActive && message.QoSLevel == 0)
            {
                return new ValueTask();
            }

            return writer.WriteAsync(message);
        }

        public override ValueTask<Message> DequeueAsync(CancellationToken cancellationToken)
        {
            return reader.ReadAsync(cancellationToken);
        }

        #endregion
    }
}