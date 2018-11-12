using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Mqtt.MqttTopicHelpers;

namespace System.Net.Mqtt.Server.Implementations
{
    public class SessionStateV3 : SessionState
    {
        private readonly IPacketIdPool idPool;
        private readonly HashSet<ushort> receivedQos2;
        private readonly HashQueue<ushort, MqttPacket> resendQueue;
        private readonly Channel<Message> sendChannel;
        private readonly Dictionary<string, byte> subscriptions;

        public SessionStateV3()
        {
            subscriptions = new Dictionary<string, byte>();
            idPool = new FastPacketIdPool();
            receivedQos2 = new HashSet<ushort>();
            resendQueue = new HashQueue<ushort, MqttPacket>();
            sendChannel = Channel.CreateUnbounded<Message>();
        }

        public bool IsActive { get; set; }

        internal byte[] Subscribe((string topic, QoSLevel qosLevel)[] topics)
        {
            var length = topics.Length;

            var result = new byte[length];

            for(var i = 0; i < length; i++)
            {
                var (topic, qos) = topics[i];

                var value = (byte)qos;
                result[i] = IsValidTopic(topic) ? subscriptions[topic] = value : (byte)0x80;
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

        public T AddResendPacket<T>(Func<ushort, T> factory) where T : MqttPacket
        {
            var id = idPool.Rent();
            var packet = factory(id);
            resendQueue.AddOrUpdate(id, packet, (_, __) => packet);
            return packet;
        }

        public IEnumerable<MqttPacket> GetResendPackets()
        {
            return resendQueue;
        }

        public void UpdateResendPacket(ushort id, MqttPacket packet)
        {
            resendQueue.AddOrUpdate(id, packet, (k, p) => packet);
        }

        public bool RemoveResendPacket(ushort id)
        {
            if(!resendQueue.TryRemove(id, out _)) return false;
            idPool.Return(id);
            return true;
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
                .Where(s => Matches(topic, s.Key))
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