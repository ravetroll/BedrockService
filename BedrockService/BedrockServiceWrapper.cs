using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Configuration;

namespace BedrockService
{
    public class BedrockServiceWrapper
    {

        Process process;
        Thread outputThread;
        Thread errorThread;
        Thread inputThread;
        static BackgroundWorker bedrockServer;
        string exePath;
        
        public BedrockServiceWrapper()
        {
            exePath = ConfigurationManager.AppSettings["BedrockServerExeLocation"];
            bedrockServer = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
          


        }
       

       
        public void Stop()
        {


            process.StandardInput.WriteLine("stop");            
            bedrockServer.CancelAsync();
            
        }

       

        public void Start()
        {
            
            bedrockServer.DoWork += (s, e) =>
            {
                

                    RunServer(exePath);
               
            };
            
            bedrockServer.RunWorkerAsync();
           

        }

        public void RunServer(string path)
        {
            
            
            // Fires up a new process to run inside this one
            process = Process.Start(new ProcessStartInfo
            {
                UseShellExecute = false,
                
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = path
                 
                
                
            });
            
            // Depending on your application you may either prioritize the IO or the exact opposite
            const ThreadPriority ioPriority = ThreadPriority.Highest;
            outputThread = new Thread(outputReader) { Name = "ChildIO Output", Priority = ioPriority };
            errorThread = new Thread(errorReader) { Name = "ChildIO Error", Priority = ioPriority };
            inputThread = new Thread(inputReader) { Name = "ChildIO Input", Priority = ioPriority };

            // Set as background threads (will automatically stop when application ends)
            outputThread.IsBackground = errorThread.IsBackground
                = inputThread.IsBackground = true;

            // Start the IO threads
            outputThread.Start(process);
            errorThread.Start(process);
            inputThread.Start(process);

            process.WaitForExit();

        }

        /// <summary>
        /// Continuously copies data from one stream to the other.
        /// </summary>
        /// <param name="instream">The input stream.</param>
        /// <param name="outstream">The output stream.</param>
        private static void passThrough(Stream instream, Stream outstream)
        {
            byte[] buffer = new byte[4096];
            while (true)
            {
                int len;
                while ((len = instream.Read(buffer, 0, buffer.Length)) > 0)
                {
                   
                    outstream.Write(buffer, 0, len);
                    outstream.Flush();
                }
                Thread.Sleep(500);
            }
        }

        private static void outputReader(object p)
        {
            var process = (Process)p;
            // Pass the standard output of the child to our standard output
            passThrough(process.StandardOutput.BaseStream, Console.OpenStandardOutput());
        }

        private static void errorReader(object p)
        {
            var process = (Process)p;
            // Pass the standard error of the child to our standard error
            passThrough(process.StandardError.BaseStream, Console.OpenStandardError());
        }

        private static void inputReader(object p)
        {
            var process = (Process)p;
            // Pass our standard input into the standard input of the child
            passThrough(Console.OpenStandardInput(), process.StandardInput.BaseStream);
        }
    }
}
