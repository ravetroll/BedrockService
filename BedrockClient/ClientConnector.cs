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
        public delegate void ConsoleWriteLine(string value);

        private static IWCFConsoleServer _server;

        public static void Connect(ConsoleWriteLine consoleWriteLine)
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
                else
                {
                    try
                    {
                        _server.GetVersion();
                        consoleWriteLine($"Connection to '{url}' established.");
                    }
                    catch(System.ServiceModel.EndpointNotFoundException)
                    {
                        consoleWriteLine($"Trying to connect to {url}");
                        _server = null;
                    }
                }
            }
            while (_server == null);
        }

        public static void OutputThread(object consoleWriteLineObject)
        {
            var consoleWriteLine = (ConsoleWriteLine)consoleWriteLineObject;

            while (true)
            {
                try
                {
                    var consoleOutput = _server.GetConsole();

                    if (string.IsNullOrWhiteSpace(consoleOutput))
                    {
                        Thread.Sleep(250);
                    }
                    else
                    {
                        consoleWriteLine(consoleOutput);
                    }
                }
                catch(System.ServiceModel.CommunicationException)
                {
                    // start connection attempts again
                    consoleWriteLine("Lost connection to server.");
                    Connect(consoleWriteLine);
                }
            }
        }

        public static void SendCommand(string command, ConsoleWriteLine consoleWriteLine)
        {
            try
            {
                _server.SendConsoleCommand(command);
            }
            catch(System.ServiceModel.CommunicationObjectFaultedException)
            {
                consoleWriteLine($"ERROR:Connection to server lost command '{command}' was not processed. Please try again.");
            }
        }
    }
}
