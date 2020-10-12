using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace BedrockService
{
    public class WCFConsoleServer : IWCFConsoleServer
    {
        public List<string> GetConsoleLine(ulong lineNumber, out ulong currentLineNumber)
        {
            currentLineNumber = 0;

            return new List<string>();
        }

        public void Start()
        {
            var binding = new NetTcpBinding();
            var baseAddress = new Uri("net.tcp://localhost:19134/MinecraftConsole");

            using (var serviceHost = new ServiceHost(typeof(WCFConsoleServer), baseAddress))
            {
                serviceHost.AddServiceEndpoint(typeof(IWCFConsoleServer), binding, baseAddress);
                serviceHost.Open();

                Console.WriteLine($"The WCF server is ready at {baseAddress}.");
                Console.WriteLine("Press <ENTER> to terminate service...");
                Console.WriteLine();
                Console.ReadLine();
            }
        }
    }
}
