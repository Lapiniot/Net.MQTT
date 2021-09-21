namespace System.Net.Mqtt;

public interface IPacketIdPool
{
    ushort Rent();
    void Release(ushort identity);
}