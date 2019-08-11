using Hyprsoft.Cloud.Utilities.HttpLogs.Stores;
using Hyprsoft.Logging.Core;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Hyprsoft.Cloud.Utilities.Tests")]
namespace Hyprsoft.IpAutoBlocker
{
    /* Wndows IoT Core Startup
    schtasks /create /tn "Hyprsoft IP Auto Blocker" /tr c:\hyprsoft\ipautoblocker\Hyprsoft.IpAutoBlocker.exe /sc onstart /ru DefaultAccount
    schtasks /delete /f /tn "Hyprsoft IP Auto Blocker"
    */

    class Program
    {
        #region Fields

        private const string AppSettingsFilename = "appsettings.json";
        private const string DataProtectionApplicationName = "Hyprsoft.IpAutoBlocker.Console";

        private static ILoggerFactory _loggerFactory;

        #endregion

        #region Methods

        static async Task Main(string[] args)
        {
            SetupLogging();
            var logger = _loggerFactory.CreateLogger<Program>();

            try
            {
                var product = (((AssemblyProductAttribute)typeof(Program).Assembly.GetCustomAttribute(typeof(AssemblyProductAttribute))).Product);
                var version = (((AssemblyInformationalVersionAttribute)typeof(Program).Assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute))).InformationalVersion);
                logger.LogInformation($"{product} v{version}");

                var settings = new AppSettings();
                var settingsFilename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), AppSettingsFilename).ToLower();
                if (File.Exists(settingsFilename))
                {
                    logger.LogInformation($"Loading app settings from '{settingsFilename}'.");
                    settings = JsonConvert.DeserializeObject<AppSettings>(await File.ReadAllTextAsync(settingsFilename));
                }
                else
                    await File.WriteAllTextAsync(settingsFilename, JsonConvert.SerializeObject(settings, Formatting.Indented));

                var ipAutoBlockerSettingsErrors = settings.IpAutoBlockerSettings.IsValid();
                var sqlServerHttpLogStoreSettingsErrors = settings.SqlServerHttpLogStoreSettings.IsValid();
                if (ipAutoBlockerSettingsErrors.Count() > 0 || sqlServerHttpLogStoreSettingsErrors.Count() > 0)
                    throw new InvalidOperationException($"The '{settingsFilename}' file is missing some settings. {string.Join(" ", ipAutoBlockerSettingsErrors.Concat(sqlServerHttpLogStoreSettingsErrors))}");

                // Encrypt our sensitive settings if this is our first run.
                if (settings.FirstRun)
                {
                    logger.LogWarning("First run detected.  Encrypting sensitive settings.");
                    settings.SqlServerHttpLogStoreSettings.ConnectionString = EncryptString(settings.SqlServerHttpLogStoreSettings.ConnectionString);
                    settings.FirstRun = false;
                    logger.LogInformation($"Saving app settings to '{settingsFilename}'.");
                    await File.WriteAllTextAsync(settingsFilename, JsonConvert.SerializeObject(settings, Formatting.Indented));
                }   // First run?
                settings.SqlServerHttpLogStoreSettings.ConnectionString = DecryptString(settings.SqlServerHttpLogStoreSettings.ConnectionString);

                using (var cts = new CancellationTokenSource())
                {
                    Console.WriteLine("\nPress Ctrl+C to exit.\n");
                    Console.CancelKeyPress += (s, e) =>
                    {
                        cts.Cancel();
                        e.Cancel = true;
                    };

                    using (var blocker = new Cloud.Utilities.Azure.IpAutoBlocker(_loggerFactory.CreateLogger<Cloud.Utilities.Azure.IpAutoBlocker>(), settings.IpAutoBlockerSettings)
                    {
                        HttpLogStore = new SqlServerHttpLogStore(settings.SqlServerHttpLogStoreSettings),
                        HttpTrafficCacheFilter = items => items.Where(x => x.Value >= 15)
                    })
                    {
                        while (!cts.Token.IsCancellationRequested)
                        {
                            try
                            {
                                var summary = await blocker.RunAsync(cts.Token);
                                logger.LogInformation($"Run Summary:\n\t" +
                                    $"Sync Interval: '{summary.SyncInterval.TotalHours}' hours (skew: '{summary.SyncIntervalSkew.TotalMinutes}' minutes)\n\t" +
                                    $"Logs Filter: '{summary.HttpLogsFilter}'\n\t" +
                                    $"Cache Filter: '{summary.HttpTrafficCacheFilter}'\n\t" +
                                    $"New HTTP Logs: '{summary.NewHttpLogEntries}'\n\t" +
                                    $"HTTP Traffic Cache: '{summary.HttpTrafficeCache.Count()}'\n\t" +
                                    $"Existing Restrictions: '{summary.Restrictions.Where(x => !x.IsNew).Count()}'\n\t" +
                                    $"New Restrictions: '{summary.Restrictions.Where(x => x.IsNew).Count()}'");
                                logger.LogInformation($"Sync completed successfully.  Next check at '{DateTime.Now.Add(settings.CheckInterval)}'.");
                            }
                            catch (TaskCanceledException)
                            {
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, $"Unexpected runtime error.  Details: {ex.Message}");
                            }
                            await Task.Delay(settings.CheckInterval, cts.Token);
                        }   // cancellation pending while loop
                    }   // using IP restrictions manager
                }   // using cancellation token source.
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Fatal application error.  Details: {ex.Message}");
                Environment.Exit(1);
            }
            logger.LogInformation($"Shutting down normally.");
        }

        private static void SetupLogging()
        {
            _loggerFactory = new LoggerFactory();
#if DEBUG
#pragma warning disable CS0618 // Type or member is obsolete
            _loggerFactory.AddDebug(LogLevel.Trace);
            _loggerFactory.AddConsole(LogLevel.Trace);
#pragma warning restore CS0618 // Type or member is obsolete
#else
            _loggerFactory.AddConsole();
            _loggerFactory.AddSimpleFileLogger();
#endif
        }

        internal static string EncryptString(string plainText)
        {
            var dp = DataProtectionProvider.Create(DataProtectionApplicationName);
            var protector = dp.CreateProtector(DataProtectionApplicationName);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(protector.Protect(plainText)));
        }

        internal static string DecryptString(string secret)
        {
            var dp = DataProtectionProvider.Create(DataProtectionApplicationName);
            var protector = dp.CreateProtector(DataProtectionApplicationName);
            return protector.Unprotect(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(secret)));
        }

        #endregion
    }
}
