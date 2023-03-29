namespace System.Net.Mqtt;

public abstract class IdentifierPool
{
    public abstract ushort Rent();
    public abstract void Return(ushort identifier);
}