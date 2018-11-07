﻿using System.Buffers;
using System.IO;
using System.Net.Mqtt.Packets;
using System.Net.Pipes;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.Packets.ConnAckPacket.StatusCodes;
using static System.Net.Mqtt.Server.Properties.Strings;
using static System.String;

namespace System.Net.Mqtt.Server.Implementations
{
    public partial class MqttServerSessionV3 : MqttServerSession<SessionStateV3>
    {
        private static readonly byte[] PingRespPacket = {0xD0, 0x00};
        private readonly WorkerLoop<object> dispatcher;
        private SessionStateV3 state;

        public MqttServerSessionV3(INetworkTransport transport, NetworkPipeReader reader,
            ISessionStateProvider<SessionStateV3> stateProvider, IObserver<Message> observer) :
            base(transport, reader, stateProvider, observer)
        {
            dispatcher = new WorkerLoop<object>(DispatchMessageAsync, null);
        }

        public bool CleanSession { get; set; }

        public string ClientId { get; set; }

        protected override async Task OnConnectAsync(CancellationToken cancellationToken)
        {
            var valueTask = MqttPacketHelpers.ReadPacketAsync(Reader, cancellationToken);
            var r = valueTask.IsCompletedSuccessfully ? valueTask.Result : await valueTask.ConfigureAwait(false);

            if(ConnectPacketV3.TryParse(r.Buffer, out var packet, out var consumed))
            {
                if(packet.ProtocolLevel != ConnectPacketV3.Level)
                {
                    await SendPacketAsync(new ConnAckPacket(ProtocolRejected), cancellationToken).ConfigureAwait(false);
                    throw new InvalidDataException(NotSupportedProtocol);
                }

                if(IsNullOrEmpty(packet.ClientId) || packet.ClientId.Length > 23)
                {
                    await SendPacketAsync(new ConnAckPacket(IdentifierRejected), cancellationToken).ConfigureAwait(false);
                    throw new InvalidDataException(InvalidClientIdentifier);
                }

                await SendPacketAsync(new ConnAckPacket(Accepted), cancellationToken).ConfigureAwait(false);
                Reader.AdvanceTo(r.Buffer.GetPosition(consumed));

                CleanSession = packet.CleanSession;
                ClientId = packet.ClientId;

                if(CleanSession)
                {
                    StateProvider.Remove(ClientId);
                    state = StateProvider.Create(ClientId);
                }
                else
                {
                    state = StateProvider.Get(ClientId) ?? StateProvider.Create(ClientId);
                }
            }
            else
            {
                throw new InvalidDataException(ConnectPacketExpected);
            }

            await base.OnConnectAsync(cancellationToken).ConfigureAwait(false);

            dispatcher.Start();
        }

        protected override Task OnDisconnectAsync()
        {
            dispatcher.Stop();
            return base.OnDisconnectAsync();
        }

        protected override Task OnConnectAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        protected override Task OnPingReqAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(header != 0b1100_0000) throw new InvalidDataException(Format(InvalidPacketTemplate, "PINGREQ"));

            return SendPacketAsync(PingRespPacket, cancellationToken);
        }

        protected override Task OnDisconnectAsync(byte header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            if(header != 0b1110_0000) throw new InvalidDataException(Format(InvalidPacketTemplate, "DISCONNECT"));

            if(CleanSession) StateProvider.Remove(ClientId);

            state.IsActive = false;

            return Transport.DisconnectAsync();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Task SendPublishResponseAsync(PacketType type, ushort id, CancellationToken cancellationToken = default)
        {
            return SendPacketAsync(new byte[] {(byte)type, 2, (byte)(id >> 8), (byte)id}, cancellationToken);
        }
    }
}