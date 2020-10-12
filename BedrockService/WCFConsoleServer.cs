using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace BedrockService
{
    public class WCFConsoleServer : IWCFConsoleServer
    {
        private static Process _process;

        ServiceHost serviceHost;

        public WCFConsoleServer()
        {
        }

        public WCFConsoleServer(Process process)
        {
            _process = process;

            var binding = new NetTcpBinding();
            var baseAddress = new Uri("net.tcp://localhost:19134/MinecraftConsole");

            serviceHost = new ServiceHost(typeof(WCFConsoleServer), baseAddress);
            serviceHost.AddServiceEndpoint(typeof(IWCFConsoleServer), binding, baseAddress);
            serviceHost.Open();
        }
        public string GetConsole()
        {
            return _process?.StandardOutput.ReadLine();
        }

        public void SendConsoleCommand(string command)
        {
            _process.StandardInput.WriteLine(command);
        }

        public void Close()
        {
            serviceHost.Close();
        }
    }
}
