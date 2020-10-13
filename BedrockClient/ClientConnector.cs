using BedrockService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace BedrockClient
{
    public class ClientConnector
    {
        public delegate void Log(string log);

        public static void SendCommand(string command, Log log)
        {
            IWCFConsoleServer server;

            var binding = new NetTcpBinding();
            var url = "net.tcp://localhost:19134/MinecraftConsole";
            var address = new EndpointAddress(url);
            var channelFactory =
                new ChannelFactory<IWCFConsoleServer>(binding, address);

            do
            {
                server = channelFactory.CreateChannel();
                if (server == null)
                {
                    log($"Trying to connect to {url}");
                }
            }
            while (server == null);

            var response = server.SendConsoleCommand(command);
            log(response);
        }
    }
}
