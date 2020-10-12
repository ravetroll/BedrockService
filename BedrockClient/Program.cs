using BedrockService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace BedrockClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var binding = new NetTcpBinding();
            var url = "net.tcp://localhost:19134/MinecraftConsole";
            var address = new EndpointAddress(url);
            var channelFactory =
                new ChannelFactory<IWCFConsoleServer>(binding, address);
            var server = channelFactory.CreateChannel();

            var list = server.GetConsoleLine(0, out ulong lineNumber);

            foreach( var item in list)
            {
                Console.WriteLine(item);
            }
        }
    }
}
