namespace System.Net.Mqtt.Server.Implementations
{
    public abstract class SessionState : IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
            }
        }
    }
}