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
        Process process;

        ServiceHost serviceHost;

        public WCFConsoleServer()
        {
        }

        public WCFConsoleServer(Process process)
        {
            this.process = process;

            var binding = new NetTcpBinding();
            var baseAddress = new Uri("net.tcp://localhost:19134/MinecraftConsole");

            serviceHost = new ServiceHost(typeof(WCFConsoleServer), baseAddress);
            serviceHost.AddServiceEndpoint(typeof(IWCFConsoleServer), binding, baseAddress);
            serviceHost.Open();
        }
        public string GetConsole()
        {
            return process?.StandardOutput.ReadToEnd();
        }

        public void SendConsoleCommand(string command)
        {
            process.StandardInput.WriteLine(command);
        }

        public void Close()
        {
            serviceHost.Close();
        }
    }
}
