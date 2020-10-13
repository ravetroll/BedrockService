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
    public class ClientConnector
    {
        public delegate void ConsoleWrite(string value);

        public static void OutputThread(object p)
        {
            var consoleWrite = (ConsoleWrite)p;

            var binding = new NetTcpBinding();
            var url = "net.tcp://localhost:19134/MinecraftConsole";
            var address = new EndpointAddress(url);
            var channelFactory =
                new ChannelFactory<IWCFConsoleServer>(binding, address);

            IWCFConsoleServer server;

            do
            {
                server = channelFactory.CreateChannel();
                if (server == null)
                {
                    Console.WriteLine($"Trying to connect to {url}");
                }
            }
            while (server == null);

            while (true)
            {
                var consoleOutput = server.GetConsole();

                if (string.IsNullOrWhiteSpace(consoleOutput))
                {
                    Thread.Sleep(250);
                }
                else
                {
                    consoleWrite(consoleOutput);
                }
            }
        }

        public static void SendCommand(string command, ConsoleWrite consoleWrite)
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
                    consoleWrite($"Trying to connect to {url}");
                }
            }
            while (server == null);

            server.SendConsoleCommand(command);
        }
    }
}
