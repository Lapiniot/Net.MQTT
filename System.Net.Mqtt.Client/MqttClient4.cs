using System.Policies;
using System.Threading;
using System.Threading.Tasks;

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

        public override byte ProtocolLevel => 0x04;

        public override string ProtocolName => "MQTT";

        protected override async Task StartingAsync(CancellationToken cancellationToken)
        {
            await StartingCoreAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task ConnectAsync(MqttConnectionOptions options, bool waitForAcknowledge, CancellationToken cancellationToken = default)
        {
            await ConnectAsync(cancellationToken).ConfigureAwait(false);

            if(waitForAcknowledge)
            {
                await WaitConnAckAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}