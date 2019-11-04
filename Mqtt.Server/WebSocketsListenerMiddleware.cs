using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Listeners;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Mqtt.Server
{
    public class WebSocketsListenerMiddleware : AsyncConnectionListener
    {
        private readonly RequestDelegate next;

        public WebSocketsListenerMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await next(context).ConfigureAwait(false);
        }

        #region Overrides of AsyncConnectionListener

        protected override IAsyncEnumerable<INetworkTransport> GetAsyncEnumerable(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}