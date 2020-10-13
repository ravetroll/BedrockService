using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BedrockService
{
    public class WCFConsoleServer : IWCFConsoleServer
    {
        public delegate string CurrentConsole();

        static Process _process;

        /// <summary>
        /// holds a call to get the console buffer
        /// </summary>
        static CurrentConsole _currentConsole;

        ServiceHost _serviceHost;

        public WCFConsoleServer()
        {
        }

        public WCFConsoleServer(Process process, CurrentConsole currentConsole)
        {
            _process = process;
            _currentConsole = currentConsole;

            var binding = new NetTcpBinding();
            var baseAddress = new Uri("net.tcp://localhost:19134/MinecraftConsole");

            _serviceHost = new ServiceHost(typeof(WCFConsoleServer), baseAddress);
            _serviceHost.AddServiceEndpoint(typeof(IWCFConsoleServer), binding, baseAddress);
            _serviceHost.Open();
        }
        public string GetConsole()
        {
            return _currentConsole();
        }

        public void SendConsoleCommand(string command)
        {
            _process.StandardInput.WriteLine(command);
        }

        public void Close()
        {
            _serviceHost.Close();
        }
    }
}
