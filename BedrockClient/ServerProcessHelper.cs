using System.Collections.Generic;
using System.Linq;
using BedrockService;

namespace BedrockClient
{
    /// <summary>
    /// Helper class to figure out which state we open the BedrockClient in
    /// </summary>
    internal class ServerProcessHelper
    {
        private readonly List<ServerInfo> _serverProcesses;

        public ServerProcessHelper(List<ServerConfig> configList)
        {
            _serverProcesses = configList.Select(x => new ServerInfo(x.WCFPortNumber)).ToList();
        }

        public void Run(string[] args)
        {
            var info = new Args(args);

            switch (info.State)
            {
                case Args.AppState.Exit:
                    _serverProcesses.ForEach(s => s.SendCommands(info));
                    break;
                case Args.AppState.Connect:
                    _serverProcesses.ForEach(s => s.Connect());
                    break;
                case Args.AppState.Init:
                    _serverProcesses.ForEach(s => s.StartProcess());
                    break;
            }
        }
    }
}
