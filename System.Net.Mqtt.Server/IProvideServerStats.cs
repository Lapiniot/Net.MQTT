namespace System.Net.Mqtt.Server;

public interface IProvideServerStats
{
    long GetTotalBytesReceived();
    long GetTotalBytesReceived(PacketType packetType);
    long GetTotalPacketsReceived();
    long GetTotalPacketsReceived(PacketType packetType);
}