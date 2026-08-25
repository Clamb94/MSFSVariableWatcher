using System.Globalization;
using FSUIPC;

namespace MSFSVariableWatcher
{
    public static class MSFSService
    {
        public static void InitMSFSServices()
        {
            // Handle events
            MSFSVariableServices.OnLogEntryReceived += VS_OnLogEntryReceived; // Fired when the WASM module sends a log entry

            MSFSVariableServices.Init(); // Initialise 
            MSFSVariableServices.LogLevel = LOGLEVEL.LOG_LEVEL_INFO; // Set the level of logging
        }

        private static void VS_OnLogEntryReceived(object? sender, LogEventArgs e)
        {
            Console.WriteLine(e.LogEntry);
        }

        public static void Start() => MSFSVariableServices.Start();

        public static void Stop() => MSFSVariableServices.Stop();

        /// <summary>
        /// Fires a K: event by handing the WASM module the equivalent calculator code,
        /// e.g. "1 2 (&gt;K:KOHLSMAN_SET)". Parameters are formatted invariant-culture.
        /// Returns the code that was sent (for display in the UI).
        /// </summary>
        public static string SendKeyEvent(string name, IEnumerable<double> parameters)
        {
            var args = string.Join(" ", parameters.Select(p => p.ToString(CultureInfo.InvariantCulture)));
            var code = string.IsNullOrEmpty(args) ? $"(>K:{name})" : $"{args} (>K:{name})";
            MSFSVariableServices.ExecuteCalculatorCode(code);
            return code;
        }

        /// <summary>
        /// Author-sanctioned recovery: Stop() resets the native data-definition state, then
        /// Start() re-attempts the connection. Existing FsLVar.OnValueChanged subscriptions
        /// survive the cycle. Safe to call even when not currently connected.
        /// </summary>
        public static void Reconnect()
        {
            MSFSVariableServices.Stop();
            MSFSVariableServices.Start();
        }
    }
}
