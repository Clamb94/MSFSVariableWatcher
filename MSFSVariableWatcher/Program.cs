using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using FSUIPC;

namespace MSFSVariableWatcher
{
    public class Program
    {
        private const string Url = "http://localhost:7672";

        public static void Main(string[] args)
        {
            // Register the native FSUIPC callbacks once. If the FSUIPC install / native DLL
            // is missing this can throw; log and continue so the web UI still comes up and
            // shows the "NOT CONNECTED" state instead of dying with a raw stack trace.
            try
            {
                MSFSService.InitMSFSServices();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialise FSUIPC services: {ex.Message}");
                Console.WriteLine("Is FSUIPC installed and is FSUIPC_WAPID.dll next to the exe?");
            }

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();

            // Connects to MSFS and reconnects automatically if the sim restarts.
            builder.Services.AddHostedService<ConnectionMonitor>();

            builder.WebHost.UseUrls(Url);

            var app = builder.Build();

            // Open the default browser once the server is listening.
            app.Lifetime.ApplicationStarted.Register(() => OpenBrowser(Url));

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }


            app.UseStaticFiles();

            app.UseRouting();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            try
            {
                app.Run();
            }
            catch (IOException ex)
            {
                // Most commonly the listen port is already taken.
                Console.WriteLine($"Could not start the web server: {ex.Message}");
                Console.WriteLine($"Port 7672 may already be in use (is MSFSVariableWatcher already running?).");
            }
        }

        private static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not open browser automatically ({ex.Message}). Open {url} manually.");
            }
        }
    }
}