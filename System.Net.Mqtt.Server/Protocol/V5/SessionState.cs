namespace System.Net.Mqtt.Server.Protocol.V5
{
    public class SessionState : V4.SessionState
    {
        public SessionState(bool persistent) : base(persistent)
        {
        }
    }
}