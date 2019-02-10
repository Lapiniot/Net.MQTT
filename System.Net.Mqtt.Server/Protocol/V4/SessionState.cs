using static System.Net.Mqtt.MqttTopicHelpers;

namespace System.Net.Mqtt.Server.Protocol.V4
{
    public class SessionState : V3.SessionState
    {
        public SessionState(string clientId, DateTime createdAt) :
            base(clientId, createdAt) {}

        protected override byte AddTopicFilterCore(string filter, byte qos)
        {
            return IsValidTopic(filter) ? Subscriptions.AddOrUpdate(filter, qos, (_, __) => qos) : (byte)0x80;
        }
    }
}