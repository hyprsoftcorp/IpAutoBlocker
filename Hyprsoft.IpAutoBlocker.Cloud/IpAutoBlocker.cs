using System;
using System.Linq;
using System.Reflection;
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
            var product = (((AssemblyProductAttribute)typeof(IpAutoBlocker).Assembly.GetCustomAttribute(typeof(AssemblyProductAttribute))).Product);
            var version = (((AssemblyInformationalVersionAttribute)typeof(IpAutoBlocker).Assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute))).InformationalVersion);
            log.LogInformation($"{product} v{version} triggered at '{DateTime.Now}'.");

            var config = new ConfigurationBuilder()
                 .SetBasePath(context.FunctionAppDirectory)
                 .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                 .AddEnvironmentVariables()
                 .Build();

            var logProviderSettings = new FtpHttpLogProviderSettings
            {
                // Required
                Host = config["Values:FtpHttpLogProviderSettings:Host"],
                Username = config["Values:FtpHttpLogProviderSettings:Username"],
                Password = config["Values:FtpHttpLogProviderSettings:Password"]
            };
            // Optional
            if (!String.IsNullOrWhiteSpace(config["FtpHttpLogProviderSettings:LogsFolder"]))
                logProviderSettings.LogsFolder = config["FtpHttpLogProviderSettings:LogsFolder"];
            if (!String.IsNullOrWhiteSpace(config["FtpHttpLogProviderSettings:AutoDeleteLogs"]))
                logProviderSettings.AutoDeleteLogs = bool.Parse(config["FtpHttpLogProviderSettings:AutoDeleteLogs"]);

            var autoBlockerSettings = new IpAutoBlockerSettings
            {
                // Required
                ClientId = config["Values:IpAutoBlockerSettings:ClientId"],
                ClientSecret = config["Values:IpAutoBlockerSettings:ClientSecret"],
                SubscriptionId = config["Values:IpAutoBlockerSettings:SubscriptionId"],
                Tenant = config["Values:IpAutoBlockerSettings:Tenant"],
                WebsiteName = config["Values:IpAutoBlockerSettings:WebsiteName"]
            };
            // Optional
            if (!String.IsNullOrWhiteSpace(config["IpAutoBlockerSettings:SyncInterval"]))
                autoBlockerSettings.SyncInterval = TimeSpan.Parse(config["IpAutoBlockerSettings:SyncInterval"]);

            var blocker = new Hyprsoft.Cloud.Utilities.Azure.IpAutoBlocker(log, autoBlockerSettings)
            {
                HttpLogProvider = new FtpHttpLogProvider(logProviderSettings),
                HttpTrafficCacheFilter = items => items.Where(x => x.Value >= 15)
            };
            var summary = await blocker.RunAsync(token);

            log.LogInformation($"Run Summary:\n\t" +
                $"Sync Interval: '{summary.SyncInterval.TotalHours}' hours\n\t" +
                $"Logs Filter: '{summary.HttpLogsFilter}'\n\t" +
                $"Cache Filter: '{summary.HttpTrafficCacheFilter}'\n\t" +
                $"New HTTP Logs: '{summary.NewHttpLogEntries}'\n\t" +
                $"HTTP Traffic Cache: '{summary.HttpTrafficeCache.Count()}'\n\t" +
                $"Existing Restrictions: '{summary.Restrictions.Where(x => !x.IsNew).Count()}'\n\t" +
                $"New Restrictions: '{summary.Restrictions.Where(x => x.IsNew).Count()}'");

            log.LogInformation($"IP Auto Blocker function exiting.  Next occurance is '{myTimer.ScheduleStatus.Next}'.");
        }
    }
}
