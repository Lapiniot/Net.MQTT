namespace System.Net.Mqtt.Server;

public interface IProvideDataStatistics
{
    long GetBytesReceived();
    long GetBytesReceived(PacketType packetType);
    long GetBytesSent();
    long GetBytesSent(PacketType packetType);
    long GetPacketsReceived();
    long GetPacketsReceived(PacketType packetType);
    long GetPacketsSent();
    long GetPacketsSent(PacketType packetType);
}