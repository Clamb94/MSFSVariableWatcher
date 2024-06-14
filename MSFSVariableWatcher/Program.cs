using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using FSUIPC;

namespace MSFSVariableWatcher
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MSFSService.InitMSFSServices();
            MSFSService.Start();

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();

            builder.WebHost.UseUrls("http://localhost:7672");

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }


            app.UseStaticFiles();

            app.UseRouting();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
        }
    }
}