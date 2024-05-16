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
    }
}
