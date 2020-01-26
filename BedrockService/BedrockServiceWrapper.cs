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
using Topshelf.Logging;
using Topshelf;

namespace BedrockService
{
    public class BedrockServiceWrapper:ServiceControl
    {

        Process process;
        Thread outputThread;
        Thread errorThread;
        Thread inputThread;
        static BackgroundWorker bedrockServer;
        string exePath;
        static readonly LogWriter _log = HostLogger.Get<BedrockServiceWrapper>();
        readonly bool _throwOnStart;
        readonly bool _throwOnStop;
        readonly bool _throwUnhandled;

        public BedrockServiceWrapper(bool throwOnStart, bool throwOnStop, bool throwUnhandled)
        {

            try
            {
                _throwOnStart = throwOnStart;
                _throwOnStop = throwOnStop;
                _throwUnhandled = throwUnhandled;
                exePath = ConfigurationManager.AppSettings["BedrockServerExeLocation"];
                bedrockServer = new BackgroundWorker
                {
                    WorkerSupportsCancellation = true
                };
            }
            catch (Exception e)
            {
                _log.Fatal("Error Instantiating BedrockServiceWrapper", e);
            }
            
        }
       
        public bool Stop(HostControl hostControl)
        {

            try
            {
                if (!(process is null))
                {
                    _log.Info("Sending Stop to Bedrock . Process.HasExited = " + process.HasExited.ToString());
                    process.StandardInput.WriteLine("stop");
                    while (!process.HasExited) { }
                    _log.Info("Sent Stop to Bedrock . Process.HasExited = " + process.HasExited.ToString());
                }
                bedrockServer.CancelAsync();
                return true;
            }
            catch (Exception e)
            {
                _log.Fatal("Error Stopping BedrockServiceWrapper", e);
                return false;
            }
        }

        public bool Start(HostControl hostControl)
        {
            try
            {
                bedrockServer.DoWork += (s, e) =>
                {
                    RunServer(exePath, hostControl);
                };
                bedrockServer.RunWorkerAsync();
                return true;
            }
            catch (Exception e)
            {
                _log.Fatal("Error Starting BedrockServiceWrapper", e);
                return false;
            }
        }

        public void RunServer(string path, HostControl hostControl)
        {

            try
            {
                if (File.Exists(path))
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
                    _log.Debug("Before process.WaitForExit()");
                    process.WaitForExit();
                    _log.Debug("After process.WaitForExit()");
                }
                else
                {
                    _log.Error("The Bedrock Server is not accessible at " + path + "\r\nCheck if the file is at that location and that permissions are correct.");
                    hostControl.Stop();
                }
            }
            catch (Exception e)
            {
                _log.Fatal("Error Running Bedrock Server", e);
                hostControl.Stop();
                
            }

        }

        /// <summary>
        /// Continuously copies data from one stream to the other.
        /// </summary>
        /// <param name="instream">The input stream.</param>
        /// <param name="outstream">The output stream.</param>
        private void passThrough(Stream instream, Stream outstream, string source)
        {
            try
            {
                byte[] buffer = new byte[4096];
                _log.Debug($"Starting passThrough for [{source}]");
                while (true)
                {
                    int len;
                    while ((len = instream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        outstream.Write(buffer, 0, len);
                        outstream.Flush();
                        _log.Debug(Encoding.ASCII.GetString(buffer).Substring(0,len).Trim());
                    }
                    Thread.Sleep(500);
                }
            }
            catch (Exception e)
            {
                _log.Fatal($"Error Sending Stream from [{source}]", e);

            }
        }

        private  void outputReader(object p)
        {
            var process = (Process)p;
            // Pass the standard output of the child to our standard output
            passThrough(process.StandardOutput.BaseStream, Console.OpenStandardOutput(),"OUTPUT");
        }

        private  void errorReader(object p)
        {
            var process = (Process)p;
            // Pass the standard error of the child to our standard error
            passThrough(process.StandardError.BaseStream, Console.OpenStandardError(),"ERROR");
        }

        private  void inputReader(object p)
        {
            var process = (Process)p;
            // Pass our standard input into the standard input of the child
            passThrough(Console.OpenStandardInput(), process.StandardInput.BaseStream,"INPUT");
        }
    }
}
