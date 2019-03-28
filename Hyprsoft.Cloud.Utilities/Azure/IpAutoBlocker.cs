using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.IO;
using System.Reflection;
using Hyprsoft.Cloud.Utilities.HttpLogs;
using System.Linq.Expressions;

[assembly: InternalsVisibleTo("Hyprsoft.Cloud.Utilities.Tests")]
namespace Hyprsoft.Cloud.Utilities.Azure
{
    public class IpAutoBlocker : IDisposable
    {
        #region Fields

        private readonly ILogger _logger;

        private bool _isDisposed;
        private HttpLogProvider _httpLogProvider;
        private IpRestrictionsProvider _ipRestrictionsProvider;
        private HttpTrafficCache _httpTrafficCache = new HttpTrafficCache();

        #endregion

        #region Constructors

        public IpAutoBlocker(ILogger logger, IpAutoBlockerSettings settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (!Settings.IsValid())
                throw new ArgumentOutOfRangeException("IP Auto Blocker settings are missing or invalid.");

            var product = (((AssemblyProductAttribute)GetType().Assembly.GetCustomAttribute(typeof(AssemblyProductAttribute))).Product);
            var version = (((AssemblyInformationalVersionAttribute)GetType().Assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute))).InformationalVersion);
            _logger.LogInformation($"{product} v{version}");

            HttpLogProvider = new LocalHttpLogProvider();
            IpRestrictionsProvider = new AppServiceIpRestrictionsProvider(settings);
        }

        #endregion

        #region Properties

        public IpAutoBlockerSettings Settings { get; }

        public HttpLogProvider HttpLogProvider
        {
            get { return _httpLogProvider; }
            set
            {
                _httpLogProvider = value;
                _httpLogProvider.Logger = _logger;
            }
        }

        public IpRestrictionsProvider IpRestrictionsProvider
        {
            get { return _ipRestrictionsProvider; }
            set
            {
                _ipRestrictionsProvider = value;
                _ipRestrictionsProvider.Logger = _logger;
            }
        }

        public Expression<Func<IEnumerable<HttpLogEntry>, IEnumerable<HttpLogEntry>>> HttpLogsFilter { get; set; } = entries => entries.Where(entry => entry.Status == HttpStatusCode.NotFound && !entry.Uri.AbsolutePath.EndsWith(".map"));

        public Expression<Func<Dictionary<string, int>, IEnumerable<KeyValuePair<string, int>>>> HttpTrafficCacheFilter { get; set; } = items => items.Where(item => item.Value >= 25);

        #endregion

        #region Methods

        public async Task<IpAutoBlockerSummary> RunAsync(CancellationToken token = default(CancellationToken))
        {
            var summary = new IpAutoBlockerSummary
            {
                SyncInterval = Settings.SyncInterval,
                HttpLogsFilter = HttpLogsFilter.ToString(),
                HttpTrafficCacheFilter = HttpTrafficCacheFilter.ToString()
            };

            _logger.LogInformation($"IP Auto Blocker running using:\n\t" +
                    $"HTTP Log Provider: '{HttpLogProvider.GetType().Name}'\n\t" +
                    $"IP Restrictions Provider: '{IpRestrictionsProvider.GetType().Name}'\n\t" +
                    $"Azure Web App: '{Settings.WebsiteName}'\n\t" +
                    $"Sync Interval: '{Settings.SyncInterval.TotalHours}' hours\n\t" +
                    $"HTTP Logs Filter: '{HttpLogsFilter.ToString()}'\n\t" +
                    $"HTTP Traffic Cache Filter: '{HttpTrafficCacheFilter.ToString()}'");

            if (!IpRestrictionsProvider.IsInitialized)
                await IpRestrictionsProvider.InitializeAsync(token).ConfigureAwait(false);
            await IpRestrictionsProvider.LoadAsync(token).ConfigureAwait(false);
            _logger.LogInformation($"Found '{IpRestrictionsProvider.Restrictions.Count}' existing IP restrictions.");

            if (!_httpTrafficCache.IsLoaded)
            {
                _logger.LogInformation("Loading HTTP traffic cache.");
                _httpTrafficCache.Load();
            }
            _logger.LogInformation($"Found '{_httpTrafficCache.Cache.Entries.Count}' HTTP traffic cache items last synced at '{_httpTrafficCache.Cache.LastSyncedUtc.ToLocalTime()}'.");

            var entries = await HttpLogProvider.GetEntriesAsync(token).ConfigureAwait(false);
            summary.NewHttpLogEntries = entries.Count();
            _logger.LogInformation($"Found '{summary.NewHttpLogEntries}' new HTTP log entries.");

            await UpdateHttpTrafficCacheAsync(entries);
            summary.HttpTrafficeCache = _httpTrafficCache.Cache.Entries.ToList();

            // If we get to this point we want to delete our logs so they aren't reprocessed later.
            if (Directory.Exists(HttpLogProvider.LocalLogsFolder))
            {
                _logger.LogInformation($"Removing local HTTP logs folder '{HttpLogProvider.LocalLogsFolder}'.");
                Directory.Delete(HttpLogProvider.LocalLogsFolder, true);
            }   // Local logs folder exists?

            if (_httpTrafficCache.Cache.LastSyncedUtc.Add(Settings.SyncInterval) <= DateTime.UtcNow)
            {
                await AddIpRestictionsAsync(token);
                await ClearHttpTrafficCacheAsync();
            }

            summary.Restrictions = IpRestrictionsProvider.Restrictions.ToList();
            return summary;
        }

        private Task UpdateHttpTrafficCacheAsync(IEnumerable<HttpLogEntry> entries)
        {
            var entiresToCache = HttpLogsFilter.Compile().Invoke(entries)
                .GroupBy(x => x.IpAddress)
                .Select(x => new { IpAddress = x.Key, Count = x.Count() });
            _logger.LogInformation($"Updating HTTP traffic cache with '{entiresToCache.Count()}' HTTP log entries.");
            foreach (var entry in entiresToCache)
            {
                if (!_httpTrafficCache.Cache.Entries.ContainsKey(entry.IpAddress))
                    _httpTrafficCache.Cache.Entries.Add(entry.IpAddress, 0);

                _httpTrafficCache.Cache.Entries[entry.IpAddress] += entry.Count;
                _logger.LogInformation($"IP address '{entry.IpAddress}' count is '{_httpTrafficCache.Cache.Entries[entry.IpAddress]}'.");
            }   // for each http log entry

            _logger.LogInformation("Saving HTTP traffic cache.");
            _httpTrafficCache.Save();

            return Task.CompletedTask;
        }

        internal Task ClearHttpTrafficCacheAsync()
        {
            _httpTrafficCache.Cache.LastSyncedUtc = DateTime.UtcNow;
            _logger.LogInformation($"Clearing HTTP traffic cache and setting last synced to '{_httpTrafficCache.Cache.LastSyncedUtc.ToLocalTime()}'.");
            _httpTrafficCache.Cache.Entries.Clear();
            _logger.LogInformation("Saving HTTP traffic cache.");
            _httpTrafficCache.Save();

            return Task.CompletedTask;
        }

        private async Task AddIpRestictionsAsync(CancellationToken token)
        {
            var items = HttpTrafficCacheFilter.Compile().Invoke(_httpTrafficCache.Cache.Entries).Where(x => !IpRestrictionsProvider.Restrictions.Any(y => y.IpAddress == x.Key)).ToList();
            _logger.LogInformation($"Creating IP restrictions for '{items.Count}' HTTP traffic cache items.");

            if (items.Count <= 0)
                return;

            foreach (var item in items)
            {
                var ipAddressCidrBlock = $"{item.Key}{AppServiceIpRestrictionsProvider.IpAddressCidrBlockSuffix}";
                _logger.LogInformation($"Adding new IP restriction for '{ipAddressCidrBlock}' with action 'Deny'.");
                IpRestrictionsProvider.Restrictions.Add(new IpRestriction
                {
                    IsNew = true,
                    IpAddress = ipAddressCidrBlock,
                    Action = "Deny",
                    Priority = 1,
                    Name = "Block"
                });
            }   // for each ip restriction
            await IpRestrictionsProvider.SaveAsync(token).ConfigureAwait(false);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            // Managed resources.
            if (disposing)
            {
                IpRestrictionsProvider?.Dispose();
            }

            // Unmanaged resources.

            _isDisposed = true;
        }

        #endregion
    }
}
