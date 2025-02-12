using System;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Turn.Server;

namespace Service
{
    static class Program
    {
        static void Main(string[] args)
        {

            var turnServer = new Turn.Server.TurnServer()
            {
                TurnUdpPort = 1567,
                TurnTcpPort = 1568,
                TurnPseudoTlsPort = 1569,
                PublicIp = IPAddress.Parse("127.0.0.1"),
                RealIp = IPAddress.Parse("127.0.0.1"),
                MinPort = 2500,
                MaxPort = 2600,
                Authentificater = new Turn.Server.Authentificater()
                {
                    Realm = "tempRealm",
                    Key1 = Encoding.ASCII.GetBytes("Key1"),
                    Key2 = Encoding.ASCII.GetBytes("Key2"),
                },
            };

            turnServer.Start();

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
