using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Mqtt.Extensions;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public class MqttServerSessionState : Server.MqttServerSessionState
    {
        private readonly ConcurrentDictionary<string, byte> subscriptions;

        public MqttServerSessionState(string clientId, DateTime createdAt) : base(clientId, createdAt)
        {
            subscriptions = new ConcurrentDictionary<string, byte>();
            IdPool = new FastPacketIdPool();
            ReceivedQos2 = new HashSet<ushort>();
            ResendQueue = new HashQueueCollection<ushort, MqttPacket>();

            (Reader, Writer) = Channel.CreateUnbounded<Message>();

            ParallelMatchThreshold = 16;
        }

        protected IPacketIdPool IdPool { get; }
        protected int ParallelMatchThreshold { get; }
        protected ChannelReader<Message> Reader { get; }
        protected HashSet<ushort> ReceivedQos2 { get; }
        protected HashQueueCollection<ushort, MqttPacket> ResendQueue { get; }
        protected ChannelWriter<Message> Writer { get; }

        public bool IsActive { get; set; }

        public Message WillMessage { get; set; }

        public bool TryAddQoS2(ushort packetId)
        {
            return ReceivedQos2.Add(packetId);
        }

        public bool RemoveQoS2(ushort packetId)
        {
            return ReceivedQos2.Remove(packetId);
        }

        public PublishPacket AddPublishToResend(string topic, Memory<byte> payload, byte qoSLevel)
        {
            var id = IdPool.Rent();
            var packet = new PublishPacket(id, qoSLevel, topic, payload, duplicate: true);
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
            IdPool.Release(id);
            return true;
        }

        public IEnumerable<MqttPacket> GetResendPackets()
        {
            return ResendQueue;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if(disposing) ResendQueue.Dispose();
        }

        #region Subscription state

        public override IReadOnlyDictionary<string, byte> GetSubscriptions()
        {
            return subscriptions;
        }

        protected override byte AddTopicFilter(string filter, byte qos)
        {
            return MqttExtensions.IsValidTopic(filter) ? AddOrUpdateInternal(filter, qos) : qos;
        }

        protected byte AddOrUpdateInternal(string filter, byte qos)
        {
            return subscriptions.AddOrUpdate(filter, qos, (_, __) => qos);
        }

        protected override void RemoveTopicFilter(string filter)
        {
            subscriptions.TryRemove(filter, out _);
        }

        #endregion

        #region Incoming message delivery state

        public override ValueTask EnqueueAsync(Message message)
        {
            if(message == null) throw new ArgumentNullException(nameof(message));

            // Skip all incoming QoS 0 if session is inactive
            if(!IsActive && message.QoSLevel == 0)
            {
                return new ValueTask();
            }

            return Writer.WriteAsync(message);
        }

        public override ValueTask<Message> DequeueAsync(CancellationToken cancellationToken)
        {
            return Reader.ReadAsync(cancellationToken);
        }

        #endregion
    }
}