using System.Collections.Generic;
using System.Net.Listeners;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace System.Net.Mqtt.Server.AspNetCore.Hosting
{
    internal class WebSocketListener : ConnectionListener, IWebSocketAcceptedNotifier
    {
        private readonly ILogger<WebSocketListener> logger;

        public WebSocketListener(ILogger<WebSocketListener> logger)
        {
            this.logger = logger;
        }

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