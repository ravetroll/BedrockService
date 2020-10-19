using BedrockService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static BedrockClient.ThreadPayLoad;

namespace BedrockClient
{
    public class ClientConnector
    {
        private static IWCFConsoleServer _server;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="consoleWriteLine"></param>
        /// <param name="portNumber">default is 19134</param>
        public static void Connect(ConsoleWriteLineDelegate consoleWriteLine, int portNumber)
        {
            var binding = new NetTcpBinding();
            var url = $"net.tcp://localhost:{portNumber}/MinecraftConsole";
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

        public static void OutputThread(object threadPayloadObject)
        {
            var threadPayload = (ThreadPayLoad)threadPayloadObject;

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
                        threadPayload.ConsoleWriteLine(consoleOutput);
                    }
                }
                catch(System.ServiceModel.CommunicationException)
                {
                    // start connection attempts again
                    threadPayload.ConsoleWriteLine("Lost connection to server.");
                    Connect(threadPayload.ConsoleWriteLine, threadPayload.PortNumber);
                }
            }
        }

        public static void SendCommand(string command, ConsoleWriteLineDelegate consoleWriteLine)
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
