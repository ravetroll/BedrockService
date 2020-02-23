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
using IniParser;
using IniParser.Model;
using IniParser.Parser;

namespace BedrockService
{
    public class BedrockServiceWrapper:ServiceControl
    {

       
        static List<BedrockServerWrapper> bedrockServers;
        

        static readonly LogWriter _log = HostLogger.Get<BedrockServiceWrapper>();
        
        HostControl _hostControl;

        const string serverProperties = "server.properties";
        const string serverName = "server-name";
        const string ipv4port = "server-port";
        const string ipv6port = "server-portv6";
        const string primaryipv4port = "19132";
        const string primaryipv6port = "19133";
        bool stopping;
        
        readonly AppSettings _settings;
        private System.Timers.Timer backupTimer;

        public BedrockServiceWrapper(bool throwOnStart, bool throwOnStop, bool throwUnhandled)
        {

            try
            {
                _settings = AppSettings.Instance;              
               
                
                bedrockServers = new List<BedrockServerWrapper>();
                _settings.ServerConfig.ForEach(t => bedrockServers.Add(new BedrockServerWrapper( t,_settings.BackupConfig)));
                if (_settings.BackupConfig.BackupOn && _settings.BackupConfig.BackupIntervalMinutes > 0)
                {
                    backupTimer = new System.Timers.Timer(_settings.BackupConfig.BackupIntervalMinutes * 60000);
                    backupTimer.Elapsed += BackupTimer_Elapsed;
                    backupTimer.Start();
                }

            }
            catch (Exception e)
            {
                _log.Fatal("Error Instantiating BedrockServiceWrapper", e);
            }
            
        }

        private void BackupTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                
                backupTimer.Stop();
                backupTimer = null;
                if (_settings.BackupConfig.BackupOn && _settings.BackupConfig.BackupIntervalMinutes > 0)
                {
                        
                    foreach (var brs in bedrockServers.OrderByDescending(t => t.ServerConfig.Primary).ToList())
                    {
                        brs.Stopping = true;                            
                        if (!stopping) brs.StopControl();
                        Thread.Sleep(1000);
                    }

                    foreach (var brs in bedrockServers.OrderByDescending(t => t.ServerConfig.Primary).ToList())
                    {
                        if (!stopping) brs.Backup();
                       
                    }
                    foreach (var brs in bedrockServers.OrderByDescending(t => t.ServerConfig.Primary).ToList())
                    {
                        brs.Stopping = false;                            
                        if (!stopping) brs.StartControl(_hostControl);
                        Thread.Sleep(2000);

                    }

                    backupTimer = new System.Timers.Timer(_settings.BackupConfig.BackupIntervalMinutes * 60000);
                    backupTimer.Elapsed += BackupTimer_Elapsed;
                    backupTimer.Start();


                }
                    
               
                
            }
            catch (Exception ex)
            {
                _log.Error("Error in BackupTimer_Elapsed", ex);
            }
        }


        public bool Stop(HostControl hostControl)
        {

            stopping = true;
            _hostControl = hostControl;
            try
            {
                foreach (var brs in bedrockServers)
                {
                    brs.Stopping = true;
                    brs.StopControl();
                    Thread.Sleep(1000);
                }
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
            
            _hostControl = hostControl;
            try
            {
                ValidSettingsCheck();
                
                foreach (var brs in bedrockServers.OrderByDescending(t => t.ServerConfig.Primary).ToList())
                {
                    brs.Stopping = false;
                    brs.StartControl(hostControl);
                    Thread.Sleep(2000);
                }
                return true;
            }
            catch (Exception e)
            {
                _log.Fatal("Error Starting BedrockServiceWrapper", e);
                return false;
            }
        }

        private void ValidSettingsCheck()
        {
            if (bedrockServers.Count() < 1)
            {
                throw new Exception("No Servers Configured");
            }
            else
            {
                var exeLocations = bedrockServers.GroupBy(t => t.ServerConfig.BedrockServerExeLocation);
                if (exeLocations.Count() != bedrockServers.Count())
                {
                    throw new Exception("Duplicate Server Paths defined");
                }
                foreach (var server in bedrockServers)
                {
                    if (!File.Exists(server.ServerConfig.BedrockServerExeLocation))
                    {
                        throw new FileNotFoundException("The bedrock server file is not accessible or does not exist", server.ServerConfig.BedrockServerExeLocation);
                        
                    }
                    else
                    {
                        FileInfo inf = new FileInfo(server.ServerConfig.BedrockServerExeLocation);
                        FileInfo configfile = inf.Directory.GetFiles(serverProperties).ToList().Single();
                        
                        IniDataParser parser = new IniDataParser();
                        parser.Configuration.AllowKeysWithoutSection = true;
                        parser.Configuration.CommentString = "#";

                        FileIniDataParser fp = new FileIniDataParser(parser);

                        IniData data = fp.ReadFile(configfile.FullName);

                        server.ServerConfig.ServerName = data.GetKey(serverName);
                        server.ServerConfig.ServerPort4 = data.GetKey(ipv4port);
                        server.ServerConfig.ServerPort6 = data.GetKey(ipv6port);

                    }
                }
                
                var duplicateV4 = bedrockServers.GroupBy(x => x.ServerConfig.ServerPort4)
                    .Where(g => g.Count() > 1)
                    .Select(y => new ServerConfig() { ServerPort4 = y.Key })
                    .ToList();
                var duplicateV4Servers = bedrockServers.Where(t => duplicateV4.Select(r => r.ServerPort4).Contains(t.ServerConfig.ServerPort4)).ToList();
                if (duplicateV4Servers.Count() > 0 )
                {
                    throw new Exception("Duplicate server IPv4 ports detected for: " + string.Join(", ", duplicateV4Servers.Select(t => t.ServerConfig.BedrockServerExeLocation)));
                }
                var duplicateV6 = bedrockServers.GroupBy(x => x.ServerConfig.ServerPort6)
                    .Where(g => g.Count() > 1)
                    .Select(y => new ServerConfig() { ServerPort6 = y.Key })
                    .ToList();
                var duplicateV6Servers = bedrockServers.Where(t => duplicateV6.Select(r => r.ServerPort6).Contains(t.ServerConfig.ServerPort6)).ToList();
                if (duplicateV6Servers.Count() > 0)
                {
                    throw new Exception("Duplicate server IPv6 ports detected for: " + string.Join(", ", duplicateV6Servers.Select(t => t.ServerConfig.BedrockServerExeLocation)));
                }
                var duplicateName = bedrockServers.GroupBy(x => x.ServerConfig.ServerName)
                    .Where(g => g.Count() > 1)
                    .Select(y => new ServerConfig() { ServerName = y.Key })
                    .ToList();
                var duplicateNameServers = bedrockServers.Where(t => duplicateName.Select(r => r.ServerName).Contains(t.ServerConfig.ServerName)).ToList();
                if (duplicateNameServers.Count() > 0)
                {
                    throw new Exception("Duplicate server names detected for: " + string.Join(", ", duplicateV6Servers.Select(t => t.ServerConfig.BedrockServerExeLocation)));
                }
                if (bedrockServers.Count > 1)
                {
                    if (!bedrockServers.Exists(t => t.ServerConfig.ServerPort4 == primaryipv4port && t.ServerConfig.ServerPort6 == primaryipv6port))
                    {
                        throw new Exception("No server defined with default ports " + primaryipv4port + " and " + primaryipv6port);
                    }
                    bedrockServers.Single(t => t.ServerConfig.ServerPort4 == primaryipv4port && t.ServerConfig.ServerPort6 == primaryipv6port).ServerConfig.Primary = true;
                }
                else
                {
                    bedrockServers.ForEach(t => t.ServerConfig.Primary = true);
                }
            }
            
        }

        

        

       


    }
}
