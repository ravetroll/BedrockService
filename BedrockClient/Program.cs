using BedrockService;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BedrockClient
{
    class Program
    {
        public static List<Process> _processList = new List<Process>();

        static void Main(string[] args)
        {
            Console.Title = "Minecraft Bedrock Service Console";
            Console.WriteLine("Minecraft Bedrock Service Console");

            if (args.Length == 0)
            {
                // we are going to get thu ports from our settings 
                List<int> portNumberList = new List<int>();

                var test = AppSettings.Instance;
                var serverConfigList = test.ServerConfig;

                foreach (var serverConfig in serverConfigList)
                {
                    Console.WriteLine($"Opening new client window for port {serverConfig.WCFPortNumber}");
                    Process process = new Process();
                    ProcessStartInfo info = new ProcessStartInfo();
                    info.FileName = "BedrockClient";
                    info.Arguments = serverConfig.WCFPortNumber.ToString();

                    process.StartInfo = info;
                    process.Start();

                    _processList.Add(process);
                }


                for( int count = 0; count < _processList.Count; count++)
                {
                    if(!_processList[count].HasExited)
                    {
                        count = 0;
                        Thread.Sleep(1000);
                    }
                }
            }
            else
            {
                if (int.TryParse(args[0], out int portNumber))
                {
                    ClientInstance(portNumber);
                }
            }
        }

        private static void ClientInstance(int portNumber)
        {
            ClientConnector.Connect(Console.WriteLine, portNumber);

            // start the connection with server to get output
            Thread outputThread = new Thread(new ParameterizedThreadStart(ClientConnector.OutputThread)) { Name = "ChildIO Output Console" };

            outputThread.Start(new ThreadPayLoad(Console.WriteLine, portNumber));

            while (true)
            {
                var command = Console.ReadLine();
                ClientConnector.SendCommand(command, Console.WriteLine);
            }
        }
    }
}
