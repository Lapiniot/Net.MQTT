using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server.Protocol.V3
{
    public class SessionState : Server.SessionState
    {
        private readonly IPacketIdPool idPool;
        private readonly HashSet<ushort> receivedQos2;
        private readonly HashQueue<ushort, MqttPacket> resendQueue;
        private readonly Channel<Message> sendChannel;
        private readonly Dictionary<string, byte> subscriptions;

        public SessionState(bool persistent)
        {
            Persistent = persistent;
            subscriptions = new Dictionary<string, byte>();
            idPool = new FastPacketIdPool();
            receivedQos2 = new HashSet<ushort>();
            resendQueue = new HashQueue<ushort, MqttPacket>();
            sendChannel = Channel.CreateUnbounded<Message>();
        }

        public bool Persistent { get; }

        public bool IsActive { get; set; }

        public Message WillMessage { get; set; }

        internal byte[] Subscribe((string topic, QoSLevel qosLevel)[] topics)
        {
            var length = topics.Length;

            var result = new byte[length];

            for(var i = 0; i < length; i++)
            {
                var (topic, qos) = topics[i];

                result[i] = MqttTopicHelpers.IsValidTopic(topic) ? subscriptions[topic] = (byte)qos : (byte)0x80;
            }

            return result;
        }

        public void Unsubscribe(string[] topics)
        {
            foreach(var topic in topics)
            {
                subscriptions.Remove(topic);
            }
        }

        public bool TryAddQoS2(ushort packetId)
        {
            return receivedQos2.Add(packetId);
        }

        public bool RemoveQoS2(ushort packetId)
        {
            return receivedQos2.Remove(packetId);
        }

        public PublishPacket AddPublishToResend(string topic, Memory<byte> payload, QoSLevel qoSLevel)
        {
            var id = idPool.Rent();
            var packet = new PublishPacket(id, qoSLevel, topic, payload);
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

        public ValueTask EnqueueAsync(Message message)
        {
            // Skip all incoming QoS 0 if session is inactive
            if(!IsActive && message.QoSLevel == QoSLevel.AtMostOnce)
            {
                return new ValueTask();
            }

            return sendChannel.Writer.WriteAsync(message);
        }

        public ValueTask<Message> DequeueAsync(CancellationToken cancellationToken)
        {
            return sendChannel.Reader.ReadAsync(cancellationToken);
        }

        public bool TopicMatches(string topic, out QoSLevel qosLevel)
        {
            var topQoS = subscriptions
                .Where(s => MqttTopicHelpers.Matches(topic, s.Key))
                .Aggregate(-1, (acc, current) => Math.Max(acc, current.Value));

            qosLevel = (QoSLevel)topQoS;
            return topQoS != -1;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if(disposing) resendQueue.Dispose();
        }
    }
}