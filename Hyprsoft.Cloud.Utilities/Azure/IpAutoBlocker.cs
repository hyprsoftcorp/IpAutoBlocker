using Hyprsoft.Cloud.Utilities.HttpLogs;
using Hyprsoft.Cloud.Utilities.HttpLogs.Providers;
using Hyprsoft.Cloud.Utilities.HttpLogs.Stores;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Hyprsoft.Cloud.Utilities.Tests")]
namespace Hyprsoft.Cloud.Utilities.Azure
{
    public class IpAutoBlocker : IDisposable
    {
        #region Fields

        private readonly ILogger _logger;

        private bool _isDisposed;
        private HttpLogProvider _httpLogProvider;
        private HttpLogStore _httpLogStore;
        private IpRestrictionsProvider _ipRestrictionsProvider;
        private HttpTrafficCache _httpTrafficCache = new HttpTrafficCache();

        #endregion

        #region Constructors

        public IpAutoBlocker(ILogger logger, IpAutoBlockerSettings settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            var errors = Settings.IsValid();
            if (errors.Count() > 0)
                throw new ArgumentOutOfRangeException($"IP Auto Blocker settings are missing or invalid. {string.Join(" ", errors)}");

            var product = (((AssemblyProductAttribute)GetType().Assembly.GetCustomAttribute(typeof(AssemblyProductAttribute))).Product);
            var version = (((AssemblyInformationalVersionAttribute)GetType().Assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute))).InformationalVersion);
            _logger.LogInformation($"{product} v{version}");

            HttpLogProvider = new LocalHttpLogProvider();
            HttpLogStore = new NoOpHttpLogStore();
            IpRestrictionsProvider = new NoOpIpRestrictionsProvider();
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

        public HttpLogStore HttpLogStore
        {
            get { return _httpLogStore; }
            set
            {
                _httpLogStore = value;
                _httpLogStore.Logger = _logger;
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

        public Expression<Func<IEnumerable<HttpLogEntry>, IEnumerable<HttpLogEntry>>> HttpLogsFilter { get; set; } = entries => entries.Where(entry => entry.Status == HttpStatusCode.NotFound && !entry.Uri.EndsWith(".map"));

        public Expression<Func<Dictionary<string, int>, IEnumerable<KeyValuePair<string, int>>>> HttpTrafficCacheFilter { get; set; } = items => items.Where(item => item.Value >= 25);

        #endregion

        #region Methods

        public async Task<IpAutoBlockerSummary> RunAsync(CancellationToken token = default(CancellationToken))
        {
            var summary = new IpAutoBlockerSummary
            {
                SyncInterval = Settings.SyncInterval,
                SyncIntervalSkew = Settings.SyncIntervalSkew,
                HttpLogsFilter = HttpLogsFilter.ToString(),
                HttpTrafficCacheFilter = HttpTrafficCacheFilter.ToString()
            };

            _logger.LogInformation($"IP Auto Blocker running using:\n\t" +
                    $"HTTP Log Provider: '{HttpLogProvider.GetType().Name}'\n\t" +
                    $"HTTP Log Store: '{HttpLogStore.GetType().Name}'\n\t" +
                    $"IP Restrictions Provider: '{IpRestrictionsProvider.GetType().Name}'\n\t" +
                    $"Sync Interval: '{Settings.SyncInterval.TotalHours}' hours (skew: '{Settings.SyncIntervalSkew.TotalMinutes}' minutes)\n\t" +
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

            if (HttpLogStore != null)
                await HttpLogStore.SaveEntriesAsync(entries, token).ConfigureAwait(false);

            await UpdateHttpTrafficCacheAsync(entries);
            summary.HttpTrafficeCache = _httpTrafficCache.Cache.Entries.ToList();

            // If we get to this point we want to delete our logs so they aren't reprocessed again.
            if (Directory.Exists(HttpLogProvider.LocalLogsFolder))
            {
                _logger.LogInformation($"Removing local HTTP logs folder '{HttpLogProvider.LocalLogsFolder}'.");
                Directory.Delete(HttpLogProvider.LocalLogsFolder, true);
            }   // Local logs folder exists?

            if (_httpTrafficCache.Cache.LastSyncedUtc.Add(Settings.SyncInterval).Subtract(Settings.SyncIntervalSkew) <= DateTime.UtcNow)
            {
                await AddIpRestictionsAsync(token);
                await ClearHttpTrafficCacheAsync();
            }

            summary.Restrictions = IpRestrictionsProvider.Restrictions.ToList();
            return summary;
        }

        private Task UpdateHttpTrafficCacheAsync(IEnumerable<HttpLogEntry> entries)
        {
            var entriesToCache = HttpLogsFilter.Compile().Invoke(entries)
                .GroupBy(x => x.IpAddress)
                .Select(x => new { IpAddress = x.Key, Count = x.Count() });
            _logger.LogInformation($"Updating HTTP traffic cache with '{entriesToCache.Count()}' HTTP log entries.");
            foreach (var entry in entriesToCache)
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
