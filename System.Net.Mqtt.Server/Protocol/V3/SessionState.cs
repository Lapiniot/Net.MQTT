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
        protected readonly IPacketIdPool IdPool;
        protected internal readonly int ParallelMatchThreshold;
        protected readonly ChannelReader<Message> Reader;
        protected readonly HashSet<ushort> ReceivedQos2;
        protected readonly HashQueue<ushort, MqttPacket> ResendQueue;
        protected readonly ConcurrentDictionary<string, byte> Subscriptions;
        protected readonly ChannelWriter<Message> Writer;

        public SessionState(string clientId, DateTime createdAt) : base(clientId, createdAt)
        {
            Subscriptions = new ConcurrentDictionary<string, byte>(1, 31);
            IdPool = new FastPacketIdPool();
            ReceivedQos2 = new HashSet<ushort>();
            ResendQueue = new HashQueue<ushort, MqttPacket>();

            var channel = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
            Writer = channel.Writer;
            Reader = channel.Reader;

            ParallelMatchThreshold = 16;
        }

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
            IdPool.Return(id);
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

        public override IDictionary<string, byte> GetSubscriptions()
        {
            return Subscriptions;
        }

        protected override byte AddTopicFilterCore(string filter, byte qos)
        {
            return IsValidTopic(filter) ? Subscriptions.AddOrUpdate(filter, qos, (_, __) => qos) : qos;
        }

        protected override void RemoveTopicFilterCore(string filter)
        {
            Subscriptions.TryRemove(filter, out _);
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

            return Writer.WriteAsync(message);
        }

        public override ValueTask<Message> DequeueAsync(CancellationToken cancellationToken)
        {
            return Reader.ReadAsync(cancellationToken);
        }

        #endregion
    }
}