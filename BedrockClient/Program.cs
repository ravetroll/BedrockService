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
        public delegate void ConsoleWrite(string value);

        private static void OutputThread(object p)
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

        static void Main(string[] args)
        {
            //// start the connection with server to get output
            //Thread outputThread = new Thread(new ParameterizedThreadStart(OutputThread)) { Name = "ChildIO Output Console" };
            //ConsoleWrite consoleWrite = Console.WriteLine;
            //outputThread.Start(consoleWrite);

            while(true)
            {
                var command = Console.ReadLine();
                ClientConnector.SendCommand(command,Console.WriteLine);
            }
        }
    }
}
