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
        private static IWCFConsoleServer _server;

        public delegate void ConsoleWrite(string value);

        private static void OutputThread(object p)
        {
            var consoleWrite = (ConsoleWrite)p;

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

        static void Main(string[] args)
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

            // start the connection with server to get output
            Thread outputThread = new Thread(new ParameterizedThreadStart(OutputThread)) { Name = "ChildIO Output Console" };
            ConsoleWrite consoleWrite = Console.WriteLine;
            outputThread.Start(consoleWrite);

            while(true)
            {
                var command = Console.ReadLine();
                _server.SendConsoleCommand(command);
            }
        }
    }
}
