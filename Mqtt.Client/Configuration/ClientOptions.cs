using System.Net.Mqtt;

namespace Mqtt.Client.Configuration;

public class ClientOptions
{
    public Uri Server { get; set; } = new Uri("tcp://127.0.0.1:1883");
    public string ClientId { get; set; }
    public int NumMessages { get; set; } = 10000;
    public int NumClients { get; set; } = 1;
    public QoSLevel QoSLevel { get; set; } = QoSLevel.QoS0;
    public TimeSpan TimeoutOverall { get; set; } = TimeSpan.FromMinutes(2);
    public string TestName { get; set; } = "publish";
}