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
            var returnValue = string.Empty;

            // this blocks the code and gets into deadlock, since I only care about sending command getting commands are going to be secondary.
            //if((_process != null ) && ( !_process.StandardOutput.EndOfStream))
            //{
            //    returnValue = _process?.StandardOutput.ReadLine();
            //}

            return returnValue;
        }

        public string SendConsoleCommand(string command)
        {
            _process.StandardInput.WriteLine(command);

            return $"Command processed";
        }

        public void Close()
        {
            serviceHost.Close();
        }
    }
}
