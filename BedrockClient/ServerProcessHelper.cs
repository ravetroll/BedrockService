using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BedrockService;

namespace BedrockClient
{
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
                    _serverProcesses.ForEach(s => s.EndProcess());
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

    internal class Args
    {
        private const string ExitParam = "-exit";
        public enum AppState
        {
            Init,
            Connect,
            Exit
        }

        public AppState State { get; }

        public Args(string[] args)
        {
            var exit = args.Any(x => x == ExitParam);
            var init = !args.Any();
            var connect = args.Any() && args.All(x => int.TryParse(x, out _));

            if (exit)
                State = AppState.Exit;
            else if (init)
                State = AppState.Init;
            else if (connect)
                State = AppState.Connect;
            else
                throw new InvalidOperationException("Invalid application state");
        }
    }

    internal class ServerInfo
    {
        private int Port { get; }

        public ServerInfo(int port)
        {
            Port = port;
        }

        public Process StartProcess()
        {
            var process = new Process();
            var info = new ProcessStartInfo {FileName = "BedrockClient", Arguments = $"{Port}"};

            process.StartInfo = info;
            process.Start();

            Console.WriteLine($"Opened new client window for port {Port}");
            return process;
        }

        public void EndProcess()
        {
            SendCommand("exit");
        }

        public void Connect()
        {
            ClientConnector.Connect(Console.WriteLine, Port);

            // start the connection with server to get output
            Thread outputThread = new Thread(ClientConnector.OutputThread) { Name = "ChildIO Output Console" };

            outputThread.Start(new ThreadPayLoad(Console.WriteLine, Port));

            while (true)
            {
                var command = Console.ReadLine();
                SendCommand(command);
            }
        }

        private void SendCommand(string command)
        {
            ClientConnector.SendCommand(command, Console.WriteLine);
        }
    }
}
