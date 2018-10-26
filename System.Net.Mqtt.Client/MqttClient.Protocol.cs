using System.Collections.Concurrent;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Client
{
    public partial class MqttClient
    {
        private readonly IPacketIdPool idPool;

        private readonly ConcurrentDictionary<ushort, TaskCompletionSource<object>> pendingCompletions;

        private async Task<bool> MqttConnectAsync(CancellationToken cancellationToken, ConnectPacket connectPacket)
        {
            await SendAsync(connectPacket.GetBytes(), cancellationToken).ConfigureAwait(false);

            Memory<byte> buffer = new byte[4];

            var received = await ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

            if(!ConnAckPacket.TryParse(buffer.Span.Slice(0, received), out var packet))
            {
                throw new InvalidDataException("Invalid CONNECT response. Valid CONNACK packet expected.");
            }

            packet.EnsureSuccessStatusCode();

            return packet.SessionPresent;
        }

        private async Task MqttDisconnectAsync()
        {
            await SendAsync(new byte[] {(byte)PacketType.Disconnect, 0}, default).ConfigureAwait(false);
        }

        private Task MqttSendPacketAsync(MqttPacket packet, CancellationToken cancellationToken = default)
        {
            return MqttSendPacketAsync(packet.GetBytes(), cancellationToken);
        }

        private async Task MqttSendPacketAsync(Memory<byte> bytes, CancellationToken cancellationToken = default)
        {
            await SendAsync(bytes, cancellationToken).ConfigureAwait(false);

            ArisePingTimer();
        }

        private async Task<T> PostPacketAsync<T>(MqttPacketWithId packet, CancellationToken cancellationToken) where T : class
        {
            var packetId = packet.Id;

            var completionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            pendingCompletions.TryAdd(packetId, completionSource);

            try
            {
                await MqttSendPacketAsync(packet, cancellationToken).ConfigureAwait(false);

                return await completionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false) as T;
            }
            finally
            {
                pendingCompletions.TryRemove(packetId, out _);
                idPool.Return(packetId);
            }
        }

        private void AcknowledgePacket(ushort packetId, object result = null)
        {
            if(pendingCompletions.TryGetValue(packetId, out var tcs))
            {
                tcs.TrySetResult(result);
            }
        }

        private async Task ResendPublishPacketsAsync()
        {
            foreach(var tuple in publishFlowPackets)
            {
                await MqttSendPacketAsync(tuple.Value).ConfigureAwait(true);
            }
        }
    }
}