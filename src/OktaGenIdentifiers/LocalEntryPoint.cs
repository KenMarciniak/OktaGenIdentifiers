using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Sentry;

namespace OktaGenIdentifiers
{
    /// <summary>
    /// The Main function can be used to run the ASP.NET Core application locally using the Kestrel webserver.
    /// </summary>
    public class LocalEntryPoint
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            //Load config file (if exists)
            //Pull the secrets from secrets manager
            //THIS NEEDS TO BE ALTERED PER SERVICE
            var secrets = new Secrets("ndpvid_test");

            //Use sentry to report errors
            //using (SentrySdk.Init(Secrets.get_secret.SentryUrl))
            {
                CreateWebHostBuilder(args).Build().Run();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>();
    }
}
