using BedrockService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
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

            while (true)
            {
                try
                {
                    var consoleOutput = server.GetConsole();
                    Console.WriteLine(consoleOutput);
                }
                catch
                {
                    server = channelFactory.CreateChannel();
                    Console.WriteLine($"Trying to connect to {url}");
                }
                Thread.Sleep(250);
            }
        }
    }
}
