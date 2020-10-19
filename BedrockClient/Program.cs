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
        static void Main(string[] args)
        {
            Console.Title = "Minecraft Bedrock Service Console";
            Console.WriteLine("Minecraft Bedrock Service Console");

            int portNumber = 19134;

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
