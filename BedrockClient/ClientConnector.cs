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

        private static IWCFConsoleServer _server;

        public static void Connect()
        {
            var binding = new NetTcpBinding();
            var url = "net.tcp://localhost:19134/MinecraftConsole";
            var address = new EndpointAddress(url);
            var channelFactory =
                new ChannelFactory<IWCFConsoleServer>(binding, address);

            do
            {
                _server = channelFactory.CreateChannel();
                if (_server == null)
                {
                    Console.WriteLine($"Trying to connect to {url}");
                }
            }
            while (_server == null);
        }

        public static void OutputThread(object consoleWriteObject)
        {
            var consoleWrite = (ConsoleWrite)consoleWriteObject;

            while (true)
            {
                var consoleOutput = _server.GetConsole();

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
            _server.SendConsoleCommand(command);
        }
    }
}
