using Hyprsoft.Cloud.Utilities.Azure;
using Hyprsoft.Cloud.Utilities.HttpLogs.Providers;
using Hyprsoft.Cloud.Utilities.HttpLogs.Stores;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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

            var autoBlockerSettings = new IpAutoBlockerSettings();
            // Optional
            if (!String.IsNullOrWhiteSpace(config["Values:IpAutoBlockerSettings:SyncInterval"]))
                autoBlockerSettings.SyncInterval = TimeSpan.Parse(config["Values:IpAutoBlockerSettings:SyncInterval"]);
            if (!String.IsNullOrWhiteSpace(config["Values:IpAutoBlockerSettings:SyncIntervalSkew"]))
                autoBlockerSettings.SyncIntervalSkew = TimeSpan.Parse(config["Values:IpAutoBlockerSettings:SyncIntervalSkew"]);

            var ftpLogProviderSettings = new FtpHttpLogProviderSettings
            {
                // Required
                Host = config["Values:FtpHttpLogProviderSettings:Host"],
                Username = config["Values:FtpHttpLogProviderSettings:Username"],
                Password = config["Values:FtpHttpLogProviderSettings:Password"]
            };
            // Optional
            if (!String.IsNullOrWhiteSpace(config["Values:FtpHttpLogProviderSettings:LogsFolder"]))
                ftpLogProviderSettings.LogsFolder = config["Values:FtpHttpLogProviderSettings:LogsFolder"];
            if (!String.IsNullOrWhiteSpace(config["Values:FtpHttpLogProviderSettings:AutoDeleteLogs"]))
                ftpLogProviderSettings.AutoDeleteLogs = bool.Parse(config["Values:FtpHttpLogProviderSettings:AutoDeleteLogs"]);

            var appServiceIpRestrictionsProviderSettings = new AppServiceIpRestrictionsProviderSettings
            {
                // Required
                ClientId = config["Values:AppServiceIpRestrictionsProviderSettings:ClientId"],
                ClientSecret = config["Values:AppServiceIpRestrictionsProviderSettings:ClientSecret"],
                SubscriptionId = config["Values:AppServiceIpRestrictionsProviderSettings:SubscriptionId"],
                Tenant = config["Values:AppServiceIpRestrictionsProviderSettings:Tenant"],
                WebsiteName = config["Values:AppServiceIpRestrictionsProviderSettings:WebsiteName"]
            };

            var sqlServerHttpLogStoreSettings = new SqlServerHttpLogStoreSettings
            {
                // Required
                ConnectionString = config["Values:SqlServerHttpLogStoreSettings:ConnectionString"]
            };

            using (var blocker = new Hyprsoft.Cloud.Utilities.Azure.IpAutoBlocker(log, autoBlockerSettings)
            {
                HttpLogProvider = new FtpHttpLogProvider(ftpLogProviderSettings),
                HttpLogStore = new SqlServerHttpLogStore(sqlServerHttpLogStoreSettings),
                IpRestrictionsProvider = new AppServiceIpRestrictionsProvider(appServiceIpRestrictionsProviderSettings),
                HttpTrafficCacheFilter = items => items.Where(x => x.Value >= 15)
            })
            {
                var summary = await blocker.RunAsync(token);

                log.LogInformation($"Run Summary:\n\t" +
                    $"Sync Interval: '{summary.SyncInterval.TotalHours}' hours (skew: '{summary.SyncIntervalSkew.TotalMinutes}' minutes)\n\t" +
                    $"Logs Filter: '{summary.HttpLogsFilter}'\n\t" +
                    $"Cache Filter: '{summary.HttpTrafficCacheFilter}'\n\t" +
                    $"New HTTP Logs: '{summary.NewHttpLogEntries}'\n\t" +
                    $"HTTP Traffic Cache: '{summary.HttpTrafficeCache.Count()}'\n\t" +
                    $"Existing Restrictions: '{summary.Restrictions.Where(x => !x.IsNew).Count()}'\n\t" +
                    $"New Restrictions: '{summary.Restrictions.Where(x => x.IsNew).Count()}'");
            }   // using ip auto blocker

            log.LogInformation($"IP Auto Blocker function exiting.  Next occurance is '{myTimer.ScheduleStatus.Next}'.");
        }
    }
}
