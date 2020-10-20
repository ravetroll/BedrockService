using BedrockService;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BedrockClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Minecraft Bedrock Service Console";
            Console.WriteLine("Minecraft Bedrock Service Console");

            if (args.Length == 0)
            {
                // we are going to get thu ports from our settings 
                List<int> portNumberList = new List<int>();

                var document = ConfigurationManager.GetSection("settings");
                //Instance = (AppSettings)serializer.Deserialize(document.CreateReader());

                var serverConfigList = AppSettings.Instance.ServerConfig;

                foreach (var serverConfig in serverConfigList)
                {
                    using (Process p = new Process())
                    {
                        ProcessStartInfo info = new ProcessStartInfo();
                        info.FileName = "BedrockClient";
                        info.Arguments = serverConfig.WCFPortNumber.ToString();
                        info.RedirectStandardInput = true;
                        info.UseShellExecute = false;

                        p.StartInfo = info;
                        p.Start();
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
