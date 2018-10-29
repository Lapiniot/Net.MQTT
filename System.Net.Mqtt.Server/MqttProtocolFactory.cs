﻿using System.IO;
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

                while(true)
                {
                    var task = reader.ReadAsync(cancellationToken);
                    var result = task.IsCompleted ? task.Result : await task.ConfigureAwait(false);
                    var buffer = result.Buffer;

                    if(TryParseHeader(buffer, out var flags, out var length, out var offset))
                    {
                        if(buffer.Length < offset + length)
                        {
                            // Not enough data received yet
                            continue;
                        }

                        if((flags & TypeMask) != (byte)Connect)
                        {
                            throw new InvalidDataException(ConnectPacketExpected);
                        }

                        if(!TryReadString(buffer.Slice(offset), out var protocol, out var consumed) || IsNullOrEmpty(protocol))
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

                        // Notify that we have not consume any data from the pipe and 
                        // cancel current pending Read operation to unblock any further 
                        // immidiate reads. Otherwise next reader will be blocked until 
                        // new portion of data is read from network socket and flushed out
                        // by writer task. Essentially, this is just a simulation of "Peek"
                        // operation in terms of pipelines API.
                        reader.AdvanceTo(buffer.Start, buffer.End);
                        reader.CancelPendingRead();

                        var args = new object[] {transport, reader};

                        return (MqttProtocol)Activator.CreateInstance(impl, BindingFlags, null, args, null);
                    }
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