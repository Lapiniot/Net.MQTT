using System.Collections.Generic;
using System.Net.Listeners;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting
{
    internal class WebSocketListenerEnumerator : ConnectionListener, IWebSocketAcceptedNotifier
    {
        #region Implementation of IWebSocketAcceptedNotifier

        public Task NotifyAcceptedAsync(WebSocket webSocket, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Overrides of AsyncConnectionListener

        protected override IAsyncEnumerable<INetworkTransport> GetAsyncEnumerable(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}