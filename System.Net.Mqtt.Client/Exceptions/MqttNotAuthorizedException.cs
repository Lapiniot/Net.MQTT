namespace System.Net.Mqtt.Client.Exceptions
{
    public class MqttNotAuthorizedException : MqttConnectException
    {
        public MqttNotAuthorizedException() : 
            base("Connection refused. Not authorized.")
        {
        }
    }
}