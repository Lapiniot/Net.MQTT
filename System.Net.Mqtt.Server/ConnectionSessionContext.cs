using System.Net.Connections;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.CompilerServices.ConfiguredTaskAwaitable;

namespace System.Net.Mqtt.Server
{
    internal struct ConnectionSessionContext : IEquatable<ConnectionSessionContext>
    {
        private Lazy<Task> completionLazy;
        private readonly INetworkConnection connection;
        private readonly MqttServerSession session;

        public ConnectionSessionContext(INetworkConnection connection, MqttServerSession session, Func<Task> startup) : this()
        {
            this.connection = connection;
            this.session = session;
            completionLazy = new Lazy<Task>(startup, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public Task Completion => completionLazy.Value;
        public INetworkConnection Connection => connection;
        public MqttServerSession Session => session;

        public void Deconstruct(out INetworkConnection connection, out MqttServerSession session)
        {
            connection = this.connection;
            session = this.session;
        }

        public override bool Equals(object obj)
        {
            return obj is ConnectionSessionContext context && Equals(context);
        }

        public bool Equals(ConnectionSessionContext other)
        {
            return ReferenceEquals(connection, other.connection) &&
                ReferenceEquals(session, other.session) &&
                ReferenceEquals(completionLazy, other.completionLazy);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(connection, session, completionLazy);
        }

        public static bool operator ==(ConnectionSessionContext ctx1, ConnectionSessionContext ctx2)
        {
            return ctx1.Equals(ctx2);
        }

        public static bool operator !=(ConnectionSessionContext ctx1, ConnectionSessionContext ctx2)
        {
            return !ctx1.Equals(ctx2);
        }

        public ConfiguredTaskAwaiter GetAwaiter()
        {
            return Completion.ConfigureAwait(false).GetAwaiter();
        }
    }
}