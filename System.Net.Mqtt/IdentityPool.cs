namespace System.Net.Mqtt;

public abstract class IdentityPool
{
    public abstract ushort Rent();
    public abstract void Release(ushort identity);
}