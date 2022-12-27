namespace System.Net.Mqtt.Server;

public interface IProvideServerStats
{
    long GetTotalBytesReceived();
    long GetTotalBytesReceived(PacketType packetType);
    long GetTotalBytesSent();
    long GetTotalBytesSent(PacketType packetType);
    long GetTotalPacketsReceived();
    long GetTotalPacketsReceived(PacketType packetType);
    long GetTotalPacketsSent();
    long GetTotalPacketsSent(PacketType packetType);
}