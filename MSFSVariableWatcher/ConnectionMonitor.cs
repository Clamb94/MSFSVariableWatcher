using FSUIPC;

namespace MSFSVariableWatcher
{
    /// <summary>
    /// Polls the FSUIPC connection and (re)connects automatically. FSUIPCClientDLL exposes
    /// no reconnect event, so we poll <see cref="MSFSVariableServices.IsRunning"/> (a live
    /// native state read) and cycle Stop()+Start() while disconnected. This also means MSFS
    /// no longer has to be running before the app launches.
    /// </summary>
    public class ConnectionMonitor : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool wasRunning = false;
            using var timer = new PeriodicTimer(PollInterval);

            // do/while so the first connection attempt happens immediately, not after 3s.
            do
            {
                try
                {
                    bool isRunning = MSFSVariableServices.IsRunning;

                    if (!isRunning)
                    {
                        if (wasRunning)
                        {
                            Console.WriteLine("MSFS connection lost, reconnecting...");
                        }
                        // Stop() resets the native data-definition state (safe even when not
                        // running); Start() re-attempts the connection. Existing FsLVar
                        // subscriptions survive this cycle.
                        MSFSService.Reconnect();
                    }
                    else if (!wasRunning)
                    {
                        Console.WriteLine("Connected to MSFS.");
                    }

                    wasRunning = isRunning;
                }
                catch (Exception ex)
                {
                    // Never let the monitor loop die; log and retry on the next tick.
                    Console.WriteLine($"Connection monitor error: {ex.Message}");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
