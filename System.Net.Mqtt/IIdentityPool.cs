namespace System.Net.Mqtt
{
    public interface IIdentityPool
    {
        ushort Rent();
        void Return(in ushort identity);
    }
}