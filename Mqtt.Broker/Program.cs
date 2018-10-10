﻿using System;
using System.Net;
using System.Net.Listeners;
using System.Net.Mqtt.Broker;
using System.Threading.Tasks;

namespace Mqtt.Broker
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName()).ConfigureAwait(false);

            var broker = new MqttBroker();

            broker.AddListener("tcp.default", new TcpSocketConnectionListener(new IPEndPoint(addresses[0], 1883)));
            broker.AddListener("ws.default", new WebSocketsConnectionListener(new Uri("ws://localhost:8000/mqtt"), "mqtt"));

            broker.Start();
        }
    }
}