using Hyprsoft.Cloud.Utilities.Azure;
using Hyprsoft.Cloud.Utilities.HttpLogs;
using Hyprsoft.Logging.Core;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Hyprsoft.Cloud.Utilities.Tests")]
namespace Hyprsoft.IpAutoBlocker
{
    /* Wndows IoT Core Startup
    schtasks /create /tn "Hyprsoft IP Auto Blocker" /tr c:\hyprsoft\ipautoblocker\Hyprsoft.IpAutoBlocker.Monitor.exe /sc onstart /ru DefaultAccount
    schtasks /delete /f /tn "Hyprsoft IP Auto Blocker"
    */

    class Program
    {
        #region Fields

        private const string AppSettingsFilename = "appsettings.json";
        private const string DataProtectionApplicationName = "Hyprsoft.IpRestrictions.Monitor";

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

                if (!settings.IpAutoBlockerSettings.IsValid() || !settings.FtpHttpLogProviderSettings.IsValid())
                    throw new InvalidOperationException($"The '{settingsFilename}' file is missing some settings.  The following settings are required: {nameof(IpAutoBlockerSettings.ClientId)}, " +
                        $"{nameof(IpAutoBlockerSettings.ClientSecret)}, {nameof(IpAutoBlockerSettings.Tenant)}, {nameof(IpAutoBlockerSettings.SubscriptionId)}, " +
                        $"{nameof(IpAutoBlockerSettings.WebsiteName)}, {nameof(FtpHttpLogProviderSettings.Host)}, {nameof(FtpHttpLogProviderSettings.Username)}, " +
                        $"{nameof(FtpHttpLogProviderSettings.Password)}, {nameof(FtpHttpLogProviderSettings.LogsFolder)}.");

                // Encrypt our sensitive settings if this is our first run.
                if (settings.FirstRun)
                {
                    logger.LogWarning("First run detected.  Encrypting sensitive settings.");
                    settings.IpAutoBlockerSettings.ClientSecret = EncryptString(settings.IpAutoBlockerSettings.ClientSecret);
                    settings.FtpHttpLogProviderSettings.Password = EncryptString(settings.FtpHttpLogProviderSettings.Password);
                    settings.FirstRun = false;
                    logger.LogInformation($"Saving app settings to '{settingsFilename}'.");
                    await File.WriteAllTextAsync(settingsFilename, JsonConvert.SerializeObject(settings, Formatting.Indented));
                }   // First run?
                settings.IpAutoBlockerSettings.ClientSecret = DecryptString(settings.IpAutoBlockerSettings.ClientSecret);
                settings.FtpHttpLogProviderSettings.Password = DecryptString(settings.FtpHttpLogProviderSettings.Password);

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
                        HttpLogProvider = new FtpHttpLogProvider(settings.FtpHttpLogProviderSettings)
                    })
                    {
                        while (!cts.Token.IsCancellationRequested)
                        {
                            try
                            {
                                await blocker.RunAsync(cts.Token);
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
