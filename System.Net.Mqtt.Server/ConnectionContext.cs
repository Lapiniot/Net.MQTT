using System.Net.Connections;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    internal struct ConnectionContext
    {
        public ConnectionContext(INetworkConnection connection, MqttServerSession session, Lazy<Task> completionLazy)
        {
            Connection = connection;
            Session = session;
            CompletionLazy = completionLazy;
        }

        public INetworkConnection Connection { get; }
        public MqttServerSession Session { get; }
        public Lazy<Task> CompletionLazy { get; }

        public void Deconstruct(out INetworkConnection connection, out MqttServerSession session, out Lazy<Task> completionLazy)
        {
            connection = Connection;
            session = Session;
            completionLazy = CompletionLazy;
        }
    }
}