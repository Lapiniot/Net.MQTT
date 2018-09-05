using System.Net.Mqtt.Messages;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.QoSLevel;
using MqttMessageMap = System.Collections.Concurrent.ConcurrentDictionary<ushort, System.Net.Mqtt.MqttMessage>;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient
    {
        private readonly MqttMessageMap pubMap = new MqttMessageMap();
        private readonly MqttMessageMap pubRecMap = new MqttMessageMap();

        public async Task PublishAsync(string topic, Memory<byte> payload,
            QoSLevel qosLevel = AtMostOnce, bool retain = false, CancellationToken token = default)
        {
            CheckConnected();

            var message = new PublishMessage(topic, payload) {QoSLevel = qosLevel, Retain = retain};

            if(qosLevel != AtMostOnce) message.PacketId = idPool.Rent();

            await MqttSendMessageAsync(message, token).ConfigureAwait(false);

            if(qosLevel == AtLeastOnce || qosLevel == ExactlyOnce)
            {
                pubMap.TryAdd(message.PacketId, message);
            }
        }
    }
}