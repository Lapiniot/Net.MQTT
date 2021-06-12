using System.Policies;

namespace System.Net.Mqtt.Client
{
    public class MqttClient4 : MqttClient3
    {
        public MqttClient4(NetworkTransport transport, string clientId = null,
            ISessionStateRepository<MqttClientSessionState> repository = null,
            IRetryPolicy reconnectPolicy = null) :
            base(clientId, transport, repository, reconnectPolicy)
        {
        }
    }
}