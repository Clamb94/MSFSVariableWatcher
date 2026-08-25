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

        private Task? connectAttempt;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // BackgroundService runs ExecuteAsync synchronously up to its first await, so
            // without this yield the connection attempt below would happen on the host
            // startup path. MSFSVariableServices.Start() blocks (and can hang indefinitely)
            // when MSFS is not running, which would stop Kestrel from ever coming up.
            await Task.Yield();

            // Init() is a blocking native call too, so it also happens here rather than in
            // Program.Main. It must run once before any Start().
            try
            {
                MSFSService.InitMSFSServices();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialise FSUIPC services: {ex.Message}");
                Console.WriteLine("Is FSUIPC installed and is FSUIPC_WAPID.dll next to the exe?");
                return;
            }

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
                        // subscriptions survive this cycle. Both are blocking native calls
                        // that can hang while MSFS is down, so they run off the poll loop and
                        // only one attempt is ever in flight.
                        if (connectAttempt is null || connectAttempt.IsCompleted)
                        {
                            connectAttempt = Task.Run(() =>
                            {
                                try
                                {
                                    MSFSService.Reconnect();
                                }
                                catch (Exception ex)
                                {
                                    // Observed here so the task never faults unobserved.
                                    Console.WriteLine($"Connection attempt failed: {ex.Message}");
                                }
                            }, stoppingToken);
                        }
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
