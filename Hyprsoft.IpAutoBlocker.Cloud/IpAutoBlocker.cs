using System;
using System.Threading;
using System.Threading.Tasks;
using Hyprsoft.Cloud.Utilities.Azure;
using Hyprsoft.Cloud.Utilities.HttpLogs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hyprsoft.IpAutoBlocker.Cloud
{
    public static class IpAutoBlocker
    {
        [FunctionName("IpAutoBlocker")]
        public static async Task Run([TimerTrigger("0 0 */8 * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext context, CancellationToken token)
        {
            log.LogInformation($"IP Auto Blocker function triggered at '{DateTime.Now}'.");

            var config = new ConfigurationBuilder()
                 .SetBasePath(context.FunctionAppDirectory)
                 .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                 .AddEnvironmentVariables()
                 .Build();

            var blocker = new Hyprsoft.Cloud.Utilities.Azure.IpAutoBlocker(log, new IpAutoBlockerSettings
            {
                ClientId = config["Values:IpAutoBlockerSettings:ClientId"],
                ClientSecret = config["Values:IpAutoBlockerSettings:ClientSecret"],
                SubscriptionId = config["Values:IpAutoBlockerSettings:SubscriptionId"],
                Tenant = config["Values:IpAutoBlockerSettings:Tenant"],
                WebsiteName = config["Values:IpAutoBlockerSettings:WebsiteName"]
            })
            {
                HttpLogProvider = new FtpHttpLogProvider(new FtpHttpLogProviderSettings
                {
                    Host = config["Values:FtpHttpLogProviderSettings:Host"],
                    Username = config["Values:FtpHttpLogProviderSettings:Username"],
                    Password = config["Values:FtpHttpLogProviderSettings:Password"],
                    LogsFolder = config["Values:FtpHttpLogProviderSettings:LogsFolder"]
                })
            };
            await blocker.RunAsync(token);

            log.LogInformation($"IP Auto Blocker function exiting.  Next occurance is '{myTimer.ScheduleStatus.Next}'.");
        }
    }
}
