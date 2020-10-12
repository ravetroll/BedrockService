using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace BedrockService
{
    [ServiceContract]
    public interface IWCFConsoleServer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lineNumber">sending 0 will return all lines</param>
        /// <returns></returns>
        [OperationContract]
        List<string> GetConsoleLine(ulong lineNumber, out ulong currentLineNumber);
    }
}
