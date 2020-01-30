
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Topshelf;

namespace BedrockService
{
    class Program
    {
        static void Main(string[] args)
        {

            XmlConfigurator.Configure();            

            var rc = HostFactory.Run(x =>                                   
            {
                x.SetStartTimeout(TimeSpan.FromSeconds(10));
                x.SetStopTimeout(TimeSpan.FromSeconds(10));
                x.UseLog4Net();
                x.UseAssemblyInfoForServiceInfo();                
                bool throwOnStart = false;
                bool throwOnStop = false;
                bool throwUnhandled = false;
                x.Service(settings => new BedrockServiceWrapper(throwOnStart, throwOnStop, throwUnhandled), s =>
                {
                    s.BeforeStartingService(_ => Console.WriteLine("BeforeStart"));
                    s.BeforeStoppingService(_ => Console.WriteLine("BeforeStop"));
                    
                });


                //x.RunAsNetworkService(); 
                x.RunAsLocalSystem();
                x.SetDescription("Windows Service Wrapper for Windows Bedrock Server");                   
                x.SetDisplayName("BedrockService");                                  
                x.SetServiceName("BedrockService");
                
            });                                                             

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());  
            Environment.ExitCode = exitCode;
            
        }
    }
}
