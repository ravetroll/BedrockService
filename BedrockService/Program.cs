using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace BedrockService
{
    class Program
    {
        static void Main(string[] args)
        {
            
            var rc = HostFactory.Run(x =>                                   
            {
                x.Service<BedrockServiceWrapper>(s =>                                  
                {
                    s.ConstructUsing(name => new BedrockServiceWrapper());                
                    s.WhenStarted(tc => tc.Start());                         
                    s.WhenStopped(tc => tc.Stop()); 
                    

                });
                x.RunAsNetworkService();                                       
                
                x.SetDescription("Windows Service Wrapper for Windows Bedrock Server");                   
                x.SetDisplayName("BedrockService");                                  
                x.SetServiceName("BedrockService");                                  
            });                                                             

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());  
            Environment.ExitCode = exitCode;
        }
    }
}
