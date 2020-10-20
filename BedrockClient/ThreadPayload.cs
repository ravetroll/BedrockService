using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockClient
{
    public class ThreadPayLoad
    {
        public delegate void ConsoleWriteLineDelegate(string value);

        public ConsoleWriteLineDelegate ConsoleWriteLine { get; set; }
        public int PortNumber { get; set; }

        public ThreadPayLoad(ConsoleWriteLineDelegate consoleWriteLine, int portNumber)
        {
            ConsoleWriteLine = consoleWriteLine;
            PortNumber = portNumber;
        }
    }
}
