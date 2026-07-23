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
