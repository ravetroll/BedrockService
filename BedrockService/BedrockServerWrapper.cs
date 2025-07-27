﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Topshelf;
using Topshelf.Logging;

namespace BedrockService
{
    public class BedrockServerWrapper
    {
        Process process;
        static readonly LogWriter _log = HostLogger.Get<BedrockServerWrapper>();
        Thread outputThread;
        Thread errorThread;
        Thread inputThread;
        string loggedThroughput;
        StringBuilder consoleBufferServiceOutput = new StringBuilder();
        bool serverStarted = false;
        
        const string worldsFolder = "worlds";
        const string startupMessage = "INFO] Server started.";
        public BedrockServerWrapper( ServerConfig serverConfig, BackupConfig backupConfig)
        {
            
            ServerConfig = serverConfig;
            BackupConfig = backupConfig;
           
        }
        public BackgroundWorker Worker { get; set; }
        public ServerConfig ServerConfig { get; set; }

        public BackupConfig BackupConfig { get; set; }
        public bool BackingUp { get; set; }
        public bool Stopping { get; set; }

        

        public void StopControl()
        {
            if (!(process is null))
            {
                _log.Info("Sending Stop to Bedrock . Process.HasExited = " + process.HasExited.ToString());
               
                process.StandardInput.WriteLine("stop");
                while (!process.HasExited) { }
                
                //_log.Info("Sent Stop to Bedrock . Process.HasExited = " + process.HasExited.ToString());
            }
            if (!(Worker is null))
            {
                Worker.CancelAsync();
                while (Worker.IsBusy) { Thread.Sleep(10); }
                Worker.Dispose();
            }
            Worker = null;
            process = null;
            GC.Collect();
            serverStarted = false;
            loggedThroughput = null;

        }

        public void StartControl(HostControl hostControl)
        {
            if (Worker is null)
            {
                Worker = new BackgroundWorker() { WorkerSupportsCancellation = true };
            }
            //while (BackingUp)
            //{
            //    Thread.Sleep(100);
            //}
            if (!Worker.IsBusy)
            {
                Worker.DoWork += (s, e) =>
                {
                    RunServer(hostControl);
                };
                Worker.RunWorkerAsync();
            }
        }

        public void RunServer(HostControl hostControl)
        {
            try
            {
                if (File.Exists(ServerConfig.BedrockServerExeLocation))
                {
                    // Fires up a new process to run inside this one
                    process = Process.Start(new ProcessStartInfo
                    {
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = ServerConfig.BedrockServerExeLocation
                    });
                    process.PriorityClass = ProcessPriorityClass.RealTime;

                    // Depending on your application you may either prioritize the IO or the exact opposite
                    const ThreadPriority ioPriority = ThreadPriority.Highest;
                    if (inputThread != null) inputThread.Interrupt();
                    if (errorThread != null) errorThread.Interrupt();
                    if (outputThread != null) outputThread.Interrupt();


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

                    _log.Debug("Starting WCF server");
                    var wcfConsoleServer = new WCFConsoleServer(process, GetCurrentConsole, ServerConfig.WCFPortNumber);

                    _log.Debug("Before process.WaitForExit()");
                    process.WaitForExit();
                    _log.Debug("After process.WaitForExit()");
                    
                    process = null;

                    _log.Debug("Stop WCF service");
                    wcfConsoleServer.Close();
                    GC.Collect();
                }
                else
                {
                    _log.Error("The Bedrock Server is not accessible at " + ServerConfig.BedrockServerExeLocation + "\r\nCheck if the file is at that location and that permissions are correct.");
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
                        consoleBufferServiceOutput.Append(Encoding.ASCII.GetString(buffer).Substring(0, len).Trim());

                        if(consoleBufferServiceOutput.Length > 10000000)
                        {
                            consoleBufferServiceOutput = new StringBuilder(consoleBufferServiceOutput.ToString().Substring(consoleBufferServiceOutput.Length - 11000000));
                        }
                        _log.Debug(Encoding.ASCII.GetString(buffer).Substring(0, len).Trim());
                        
                        if (!serverStarted)
                        {
                            loggedThroughput += Encoding.ASCII.GetString(buffer).Substring(0, len).Trim();
                            if (loggedThroughput.Contains(startupMessage))
                            {
                                serverStarted = true;
                                RunStartupCommands();
                            }
                        }
                    }
                    Thread.Sleep(100);

                }
            }
            catch (ThreadInterruptedException e)
            {
                _log.Debug($"Interrupting thread from [{source}]", e);
            }
            catch (ThreadAbortException e)
            {
                _log.Info($"Aborting thread from [{source}]", e);
            }
            catch (Exception e)
            {
                _log.Fatal($"Error Sending Stream from [{source}]", e);

            }
        }

        private void outputReader(object p)
        {
            var process = (Process)p;
            // Pass the standard output of the child to our standard output
            passThrough(process.StandardOutput.BaseStream, Console.OpenStandardOutput(), "OUTPUT");
        }

        private void errorReader(object p)
        {
            var process = (Process)p;
            // Pass the standard error of the child to our standard error
            passThrough(process.StandardError.BaseStream, Console.OpenStandardError(), "ERROR");
        }

        private void inputReader(object p)
        {
            var process = (Process)p;
            // Pass our standard input into the standard input of the child
            passThrough(Console.OpenStandardInput(), process.StandardInput.BaseStream, "INPUT");
        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public void Backup()
        {
            if (string.IsNullOrEmpty(this.ServerConfig.BackupFolderName))
            {
                return;
            }

            try
            {

                BackingUp = true;

                process.StandardInput.WriteLine("save hold");
                Thread.Sleep(100);

                List<BackupFile> backupFiles;
                while (true)
                {
                    process.StandardInput.WriteLine("save query");
                    Thread.Sleep(100);
                    var lastLine = GetLastConsoleLine();
                    if (BackupFile.IsBackupSpecification(lastLine))
                    {
                        backupFiles = BackupFile.ParseBackupSpecification(lastLine);
                        break;
                    }
                }

                FileInfo exe = new FileInfo(ServerConfig.BedrockServerExeLocation);

                DirectoryInfo backupTo;
                if (Directory.Exists(ServerConfig.BackupFolderName))
                {
                    backupTo = new DirectoryInfo(ServerConfig.BackupFolderName);
                }
                else if (exe.Directory.GetDirectories().Count(t => t.Name == ServerConfig.BackupFolderName) == 1)
                {
                    backupTo = exe.Directory.GetDirectories().Single(t => t.Name == ServerConfig.BackupFolderName);
                }
                else
                {
                    backupTo = exe.Directory.CreateSubdirectory(ServerConfig.BackupFolderName);
                }

                var sourceDirectory = exe.Directory.GetDirectories().Single(t => t.Name == worldsFolder);
                var targetDirectory = backupTo.CreateSubdirectory($"{worldsFolder}{DateTime.Now:yyyyMMddHHmmss}");
                CopyBackupFiles(sourceDirectory, targetDirectory, backupFiles);
            }
            catch (Exception e)
            {
                _log.Error($"Error with Backup", e);
            }
            finally
            {
                process.StandardInput.WriteLine("save resume");
                BackingUp = false;
            }
        }

        private static void CopyBackupFiles(DirectoryInfo source, DirectoryInfo target, List<BackupFile> backupFiles)
        {
            _log.Info("Starting Backup");

            foreach (var file in backupFiles)
            {
                var destFileName = Path.Combine(target.FullName, file.Path);

                // Make sure that target directory exists
                new FileInfo(destFileName).Directory.Create();

                File.Copy(Path.Combine(source.FullName, file.Path), destFileName);

                // This trims file to specified length
                new FileStream(destFileName, FileMode.Open).SetLength(file.Length);
            }

            _log.Info("Finished Backup");
        }

        private void RunStartupCommands()
        {
            foreach (string s in ServerConfig.StartupCommands.CommandText)
            {
                process.StandardInput.WriteLine(s.Trim());
                Thread.Sleep(1000);
            }
        }

        public string GetCurrentConsole()
        {
            var sendConsole = consoleBufferServiceOutput.ToString();

            // clear out the buffer
            consoleBufferServiceOutput = new StringBuilder();

            return sendConsole;
        }

        private string GetLastConsoleLine()
        {
            return GetCurrentConsole()
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault();
        }
    }
}
