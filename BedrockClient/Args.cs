using System;
using System.Collections.Generic;
using System.Linq;

namespace BedrockClient
{
    /// <summary>
    /// Figure out the current application state (initial window, connected client window, exit with params)
    /// </summary>
    internal class Args
    {
        private const string ExitParamsLookupValue = "-";

        public enum AppState
        {
            Init,
            Connect,
            Exit
        }

        public AppState State { get; }

        public List<string> ExitParams { get; }

        public int PortParam { get; }

        public Args(string[] args)
        {
            ExitParams = Map(args);
            var exit = ExitParams.Any();
            var init = !args.Any();
            var port = 0;
            var connect = args.Length == 1 && int.TryParse(args.First(), out port);

            if (exit)
            {
                State = AppState.Exit;
            }
            else if (init)
            {
                State = AppState.Init;
            }
            else if (connect)
            {
                State = AppState.Connect;
                PortParam = port;
            }
            else
                throw new InvalidOperationException("Invalid application state");
        }

        /// <summary>
        /// Look for arguments starting with a hyphen
        /// </summary>
        /// <param name="args">Application arguments</param>
        /// <returns>List of arguments without the hyphen</returns>
        private static List<string> Map(string[] args)
        {
            return args
                .Where(x => x.Contains(ExitParamsLookupValue))
                .Select(x => x.Substring(x.IndexOf(ExitParamsLookupValue, StringComparison.Ordinal) + 1))
                .ToList();
        }
    }
}