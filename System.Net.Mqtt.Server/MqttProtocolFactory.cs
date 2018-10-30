using System.IO;
using System.Linq;
using System.Net.Pipes;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mqtt.Server.Properties.Strings;
using static System.Net.Mqtt.MqttHelpers;
using static System.Net.Mqtt.PacketFlags;
using static System.Net.Mqtt.PacketType;
using static System.Reflection.BindingFlags;
using static System.String;

namespace System.Net.Mqtt.Server
{
    public class MqttProtocolFactory
    {
        protected internal const BindingFlags BindingFlags = Instance | NonPublic | Public;
        private readonly (byte Version, Type Type)[] protocols;

        public MqttProtocolFactory(params (byte Version, Type Type)[] protocols)
        {
            this.protocols = protocols;
        }

        public async Task<MqttProtocol> DetectProtocolAsync(
            INetworkTransport transport, CancellationToken cancellationToken)
        {
            var reader = new NetworkPipeReader(transport);

            try
            {
                await reader.ConnectAsync(cancellationToken).ConfigureAwait(false);

                var (flags, _, offset) = await MqttPacketHelpers.PeekAsync(reader, cancellationToken).ConfigureAwait(false);

                if((flags & TypeMask) != (byte)Connect)
                {
                    throw new InvalidDataException(ConnectPacketExpected);
                }

                var task = reader.ReadAsync(cancellationToken);
                var result = task.IsCompleted ? task.Result : await task.ConfigureAwait(false);
                var buffer = result.Buffer;

                try
                {
                    if(!TryReadString(buffer.Slice(offset), out var protocol, out var consumed) ||
                       IsNullOrEmpty(protocol))
                    {
                        throw new InvalidDataException(ProtocolNameExpected);
                    }

                    if(!TryReadByte(buffer.Slice(offset + consumed), out var version))
                    {
                        throw new InvalidDataException(ProtocolVersionExpected);
                    }

                    var impl = protocols.FirstOrDefault(i => i.Version == version).Type;

                    if(impl == null)
                    {
                        throw new InvalidDataException(NotSupportedProtocol);
                    }

                    return (MqttProtocol)Activator.CreateInstance(impl, BindingFlags, null,
                        new object[] {transport, reader}, null);
                }
                finally
                {
                    reader.AdvanceTo(buffer.Start, buffer.End);
                    reader.CancelPendingRead();
                }
            }
            catch
            {
                reader.Dispose();
                throw;
            }
        }
    }
}