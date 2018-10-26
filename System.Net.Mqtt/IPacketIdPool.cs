namespace System.Net.Mqtt
{
    public interface IPacketIdPool
    {
        ushort Rent();
        void Return(in ushort identity);
    }
}